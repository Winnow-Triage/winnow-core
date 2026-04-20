#!/bin/bash
cd /home/jamesstubbington/projects/software/winnow-secure/winnow/winnow-core

# Ensure background workers are completely dead
pkill -f "dotnet run --project src/Services/Winnow.Sanitize/Winnow.Sanitize.csproj"
pkill -f "dotnet run --project src/Services/Winnow.API/Winnow.API.csproj"
sleep 2

export ASPNETCORE_ENVIRONMENT=Development
export ASPNETCORE_URLS="http://localhost:5294"
export ConnectionStrings__Postgres="Host=localhost;Port=5432;Database=winnow_db;Username=winnow;Password=localpassword"
export ConnectionStrings__Redis="localhost:6379"
export S3Settings__Endpoint="http://localhost:4566"
export RABBITMQ_HOST="localhost"
export LlmSettings__Presidio__AnalyzerEndpoint="http://localhost:5002"
export LlmSettings__Provider="None"
export EmailSettings__Provider="None"
export Encryption__MasterKey="NF9Jh9TwWEbOgVCkKiEwbXcRHhyKPVHhIMeUzDn0zYg="

ASPNETCORE_URLS="http://localhost:5295" dotnet run --project src/Services/Winnow.Sanitize/Winnow.Sanitize.csproj --configuration Release --no-build > current_sanitize_run.log 2>&1 &
SANITIZE_PID=$!

sleep 7

cat current_sanitize_run.log

kill $SANITIZE_PID
