#!/bin/bash
echo "Initializing LocalStack resources..."

# Create S3 buckets
awslocal s3 mb s3://winnow-quarantine
awslocal s3 mb s3://winnow-clean

echo "LocalStack initialization complete!"
