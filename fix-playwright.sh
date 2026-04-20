#!/bin/bash
sed -i 's/npx playwright install --with-deps chromium/npx playwright install chromium --with-deps/g' .github/workflows/ci.yml
