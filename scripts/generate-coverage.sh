#!/bin/bash

# Script to generate code coverage reports for Winnow.API.Tests
# Generates coverage data and HTML reports for both unit and integration tests

set -e  # Exit on any error

rm -rf ./coverage ./coverage-report

echo "=== Generating Code Coverage Report ==="

# 1. Install report generator tool (ignore if already installed)
echo "Installing dotnet-reportgenerator-globaltool..."
dotnet tool install -g dotnet-reportgenerator-globaltool 2>/dev/null || echo "Report generator already installed or installation failed (continuing...)"

# 2. Run tests with coverage collection
echo "Running tests with coverage collection..."
dotnet test src/Services/Winnow.API.Tests \
    --collect:"XPlat Code Coverage" \
    --results-directory ./coverage \
    --settings:src/Services/Winnow.API.Tests/coverlet.runsettings

# 3. Generate HTML report
echo "Generating HTML report..."
reportgenerator \
    "-reports:./coverage/**/coverage.cobertura.xml" \
    -targetdir:./coverage-report \
    -reporttypes:Html \
    -verbosity:Info

echo "=== Coverage Report Generated Successfully ==="
echo ""
echo "Coverage data saved to: ./coverage/"
echo "HTML report saved to:   ./coverage-report/"
echo ""
echo "To view the report, open: ./coverage-report/index.html in your browser"
echo "Or run: open ./coverage-report/index.html (macOS) or xdg-open ./coverage-report/index.html (Linux)"