#!/bin/bash

# run-all-tests.sh
# Script to run all tests for the Winnow project (backend and frontend)
# Exits with code 1 if any test suite fails

set -e

echo "=== Running Winnow Test Suite ==="
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to print colored status
print_status() {
    echo -e "${YELLOW}[$1]${NC} $2"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Track overall success
OVERALL_SUCCESS=true

# ====================
# 1. BACKEND TESTS (.NET)
# ====================
print_status "BACKEND" "Running .NET server tests..."
cd src/Services/Winnow.Server.Tests

if dotnet test --verbosity minimal --logger "console;verbosity=minimal"; then
    print_success "Backend tests passed"
else
    print_error "Backend tests failed"
    OVERALL_SUCCESS=false
fi

cd ../../..

# ====================
# 2. FRONTEND TESTS (Vitest)
# ====================
print_status "FRONTEND" "Running client tests..."
cd src/Apps/Winnow.Client

# Run tests and capture output
FRONTEND_OUTPUT=$(npm run test -- --run 2>&1) || FRONTEND_EXIT_CODE=$?
FRONTEND_EXIT_CODE=${FRONTEND_EXIT_CODE:-0}

# Show the last 10 lines of test output for visibility
echo "Frontend test output:"
echo "---------------------"
echo "$FRONTEND_OUTPUT" | tail -15
echo "---------------------"

# Check if tests passed
if [ $FRONTEND_EXIT_CODE -eq 0 ] && echo "$FRONTEND_OUTPUT" | grep -q "Test Files.*passed\|Test Files.*failed"; then
    # Check for any failures in the output
    if echo "$FRONTEND_OUTPUT" | grep -q "failed\|FAIL"; then
        print_error "Frontend tests failed"
        OVERALL_SUCCESS=false
    else
        print_success "Frontend tests passed"
    fi
else
    if [ $FRONTEND_EXIT_CODE -eq 0 ]; then
        print_success "Frontend tests passed"
    else
        print_error "Frontend tests failed"
        OVERALL_SUCCESS=false
    fi
fi

cd ../../..

echo ""
echo "=== Test Summary ==="

if [ "$OVERALL_SUCCESS" = true ]; then
    echo -e "${GREEN}✓ All test suites passed${NC}"
    echo ""
    exit 0
else
    echo -e "${RED}✗ Some test suites failed${NC}"
    echo ""
    exit 1
fi