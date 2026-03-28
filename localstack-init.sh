#!/bin/bash
echo "Initializing LocalStack resources..."

# Create S3 buckets for asset scanning
awslocal s3 mb s3://winnow-quarantine
awslocal s3 mb s3://winnow-clean

# Create S3 bucket for security definitions
awslocal s3 mb s3://winnow-security-defs

echo "LocalStack initialization complete!"
