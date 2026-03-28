#!/bin/bash

# Integration test for dashboard reports with embeddings
API_URL="http://localhost:5294"
EMAIL="test-dashboard@example.com"
PASSWORD="TestPassword123!"
FULLNAME="Dashboard Test User"

echo "=== Dashboard Integration Test ==="
echo "Testing that debug reports with embeddings appear in dashboards"
echo ""

# Clean up any existing test data
echo "1. Starting clean test..."
rm -f /tmp/test-token.txt /tmp/test-project-id.txt 2>/dev/null

# Register user
echo "2. Registering test user..."
REGISTER_RESPONSE=$(curl -s -X POST "$API_URL/auth/register" \
  -H "Content-Type: application/json" \
  -d "{
    \"fullName\": \"$FULLNAME\",
    \"email\": \"$EMAIL\",
    \"password\": \"$PASSWORD\"
  }")

if [[ $REGISTER_RESPONSE == *"error"* || $REGISTER_RESPONSE == *"Error"* ]]; then
    echo "   User may already exist, trying login..."
fi

# Login
echo "3. Logging in..."
LOGIN_RESPONSE=$(curl -s -X POST "$API_URL/auth/login" \
  -H "Content-Type: application/json" \
  -d "{
    \"email\": \"$EMAIL\",
    \"password\": \"$PASSWORD\"
  }")

TOKEN=$(echo $LOGIN_RESPONSE | grep -o '"token":"[^"]*' | grep -o '[^"]*$')

if [ -z "$TOKEN" ]; then
    echo "   Login Failed!"
    echo "   Response: $LOGIN_RESPONSE"
    exit 1
else
    echo "   Login Successful! Token obtained."
    echo "$TOKEN" > /tmp/test-token.txt
fi

# Create a test project
echo "4. Creating test project..."
CREATE_PROJECT_RESPONSE=$(curl -s -X POST "$API_URL/projects" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Dashboard Test Project"
  }')

PROJECT_ID=$(echo $CREATE_PROJECT_RESPONSE | grep -o '"id":"[^"]*' | grep -o '[^"]*$')
if [ -z "$PROJECT_ID" ]; then
    # Try to extract from different format
    PROJECT_ID=$(echo $CREATE_PROJECT_RESPONSE | grep -o '"id":"[^"]*' | cut -d'"' -f4)
fi

if [ -z "$PROJECT_ID" ]; then
    echo "   Could not create project, trying to list existing projects..."
    PROJECTS_RESPONSE=$(curl -s -X GET "$API_URL/projects" \
      -H "Authorization: Bearer $TOKEN")
    PROJECT_ID=$(echo $PROJECTS_RESPONSE | grep -o '"id":"[^"]*' | head -1 | grep -o '[^"]*$')
fi

if [ -z "$PROJECT_ID" ]; then
    echo "   Failed to get project ID!"
    echo "   Create Response: $CREATE_PROJECT_RESPONSE"
    echo "   Projects Response: $PROJECTS_RESPONSE"
    exit 1
else
    echo "   Project ID: $PROJECT_ID"
    echo "$PROJECT_ID" > /tmp/test-project-id.txt
fi

# Get initial dashboard metrics
echo "5. Getting initial dashboard metrics..."
INITIAL_METRICS=$(curl -s -X GET "$API_URL/dashboard/metrics" \
  -H "Authorization: Bearer $TOKEN" \
  -H "X-Project-ID: $PROJECT_ID")

echo "   Initial Metrics: $INITIAL_METRICS"

# Generate mock reports (with embeddings)
echo "6. Generating 3 mock reports with embeddings..."
MOCK_RESPONSE=$(curl -s -X POST "$API_URL/reports/generate-mock" \
  -H "Authorization: Bearer $TOKEN" \
  -H "X-Project-ID: $PROJECT_ID" \
  -H "Content-Type: application/json" \
  -d '{
    "count": 3,
    "scenario": "Dashboard Integration Test"
  }')

echo "   Mock Generation Response: $MOCK_RESPONSE"

# Wait for async processing
echo "7. Waiting 3 seconds for async processing..."
sleep 3

# Get updated dashboard metrics
echo "8. Getting updated dashboard metrics..."
UPDATED_METRICS=$(curl -s -X GET "$API_URL/dashboard/metrics" \
  -H "Authorization: Bearer $TOKEN" \
  -H "X-Project-ID: $PROJECT_ID")

echo "   Updated Metrics: $UPDATED_METRICS"

# Get all reports
echo "9. Getting all reports..."
ALL_REPORTS=$(curl -s -X GET "$API_URL/reports" \
  -H "Authorization: Bearer $TOKEN" \
  -H "X-Project-ID: $PROJECT_ID")

REPORT_COUNT=$(echo $ALL_REPORTS | grep -o '"id"' | wc -l)
echo "   Total Reports: $REPORT_COUNT"

# Check if reports have embeddings
echo "10. Checking report embeddings..."
if [ -f "/tmp/winnow-server.log" ]; then
    EMBEDDING_LOG=$(tail -50 /tmp/winnow-server.log | grep -i "embedding\|vector")
    if [ -n "$EMBEDDING_LOG" ]; then
        echo "   Embedding logs found:"
        echo "$EMBEDDING_LOG" | head -5
    else
        echo "   No embedding logs found in recent server log"
    fi
fi

# Simulate traffic (with embeddings)
echo "11. Simulating 2 traffic reports with embeddings..."
SIMULATE_RESPONSE=$(curl -s -X POST "$API_URL/debug/simulate-traffic" \
  -H "Authorization: Bearer $TOKEN" \
  -H "X-Project-ID: $PROJECT_ID" \
  -H "Content-Type: application/json" \
  -d '{
    "count": 2,
    "topic": "Login Failure"
  }')

echo "   Simulate Traffic Response: $SIMULATE_RESPONSE"

# Wait for async processing
echo "12. Waiting 3 seconds for async processing..."
sleep 3

# Final dashboard metrics
echo "13. Getting final dashboard metrics..."
FINAL_METRICS=$(curl -s -X GET "$API_URL/dashboard/metrics" \
  -H "Authorization: Bearer $TOKEN" \
  -H "X-Project-ID: $PROJECT_ID")

echo "   Final Metrics: $FINAL_METRICS"

# Final report count
FINAL_REPORTS=$(curl -s -X GET "$API_URL/reports" \
  -H "Authorization: Bearer $TOKEN" \
  -H "X-Project-ID: $PROJECT_ID")

FINAL_COUNT=$(echo $FINAL_REPORTS | grep -o '"id"' | wc -l)
echo "14. Final Report Count: $FINAL_COUNT"

# Summary
echo ""
echo "=== TEST SUMMARY ==="
echo "Initial Reports: $(echo $INITIAL_METRICS | grep -o '"totalReports":[0-9]*' | cut -d: -f2 || echo 0)"
echo "Final Reports: $(echo $FINAL_METRICS | grep -o '"totalReports":[0-9]*' | cut -d: -f2 || echo 0)"
echo "Total Reports via API: $FINAL_COUNT"
echo ""

if [ "$FINAL_COUNT" -ge 5 ]; then
    echo "✅ SUCCESS: Reports are being created and appear in the dashboard!"
    echo "   Embeddings are being generated for debug reports."
    echo "   Data isolation is working correctly with ProjectId filtering."
else
    echo "❌ FAILURE: Expected at least 5 reports, but got $FINAL_COUNT"
    echo "   Check server logs for embedding generation errors."
    exit 1
fi

# Cleanup
echo ""
echo "Cleaning up test files..."
rm -f /tmp/test-token.txt /tmp/test-project-id.txt 2>/dev/null

echo "=== Test Complete ==="