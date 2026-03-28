#!/bin/bash

# Configuration - use environment variables for secrets
API_URL="${WINNOW_API_URL:-http://localhost:5294}"
EMAIL="${WINNOW_TEST_EMAIL:-testuser@example.com}"
PASSWORD="${WINNOW_TEST_PASSWORD:-}"
FULLNAME="${WINNOW_TEST_FULLNAME:-Test User}"

# Exit if required password is not set
if [ -z "$PASSWORD" ]; then
    echo "ERROR: WINNOW_TEST_PASSWORD environment variable is not set."
    echo "Please set WINNOW_TEST_PASSWORD before running this script."
    echo "Example: export WINNOW_TEST_PASSWORD='YourSecurePassword123!'"
    exit 1
fi

echo "1. Registering User..."
REGISTER_RESPONSE=$(curl -s -X POST "$API_URL/auth/register" \
  -H "Content-Type: application/json" \
  -d "{
    \"fullName\": \"$FULLNAME\",
    \"email\": \"$EMAIL\",
    \"password\": \"$PASSWORD\"
  }")

# Check if registration failed (already exists or other error)
if [[ $REGISTER_RESPONSE == *"error"* || $REGISTER_RESPONSE == *"Error"* ]]; then
    echo "Registration failed or user already exists. Trying login..."
else
    echo "Registration Successful!"
    echo "Response: $REGISTER_RESPONSE"
fi

echo -e "\n2. Logging In..."
LOGIN_RESPONSE=$(curl -s -X POST "$API_URL/auth/login" \
  -H "Content-Type: application/json" \
  -d "{
    \"email\": \"$EMAIL\",
    \"password\": \"$PASSWORD\"
  }")

TOKEN=$(echo $LOGIN_RESPONSE | grep -o '"token":"[^"]*' | grep -o '[^"]*$')

if [ -z "$TOKEN" ]; then
    echo "Login Failed!"
    echo "Response: $LOGIN_RESPONSE"
    exit 1
else
    echo "Login Successful!"
    echo "Token received (truncated): ${TOKEN:0:20}..."
fi

echo -e "\n3. Listing Projects (Protected Endpoint)..."
PROJECTS_RESPONSE=$(curl -s -X GET "$API_URL/projects" \
  -H "Authorization: Bearer $TOKEN")

echo "Projects: $PROJECTS_RESPONSE"

echo -e "\n4. Creating New Project..."
CREATE_PROJECT_RESPONSE=$(curl -s -X POST "$API_URL/projects" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "New Project"
  }')
echo "Create Response: $CREATE_PROJECT_RESPONSE"

if [[ $PROJECTS_RESPONSE == *"id"* ]]; then
    echo -e "\nSUCCESS: Authentication and Authorization working!"
else
    echo -e "\nFAILURE: Could not list projects."
fi