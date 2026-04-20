#!/bin/bash
cd /home/jamesstubbington/projects/software/winnow-secure/winnow/winnow-core

export ASPNETCORE_ENVIRONMENT=Development
export ASPNETCORE_URLS="http://localhost:5294"
export ConnectionStrings__Postgres="Host=localhost;Port=5432;Database=winnow_db;Username=winnow;Password=localpassword"
export S3Settings__Endpoint="http://localhost:4566"
export RABBITMQ_HOST="localhost"
export LlmSettings__Presidio__AnalyzerEndpoint="http://localhost:5002"
export LlmSettings__Provider="None"
export EmailSettings__Provider="None"
export Encryption__MasterKey="NF9Jh9TwWEbOgVCkKiEwbXcRHhyKPVHhIMeUzDn0zYg="
export AppUrl="http://localhost:5173"

ASPNETCORE_URLS="http://localhost:5295" dotnet run --project src/Services/Winnow.Sanitize/Winnow.Sanitize.csproj --configuration Release --no-build > sanitize_current.log 2>&1 &
SANITIZE_PID=$!

dotnet run --project src/Services/Winnow.API/Winnow.API.csproj --configuration Release --no-build > api_current.log 2>&1 &
API_PID=$!

echo "Waiting for API to start on port 5294..."
for i in $(seq 1 30); do
  if curl -sf http://localhost:5294/health/live > /dev/null 2>&1; then
    echo "✅ API is ready!"
    break
  fi
  sleep 2
done

cd src/Apps/Winnow.Client
export VITE_API_URL="http://localhost:5294"
npm run test:e2e:full -- -g "Triage Flow"
E2E_EXIT=$?

kill $SANITIZE_PID $API_PID

exit $E2E_EXIT
