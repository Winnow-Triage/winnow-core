#!/bin/bash

echo "Creating SQS Queue in Ministack..."
# Use aws --endpoint-url inside the container to reliably provision S3 and SQS
podman-compose exec ministack aws --endpoint-url http://localhost:4566 sqs create-queue --queue-name winnow-quarantine-queue

# Get the Queue ARN so we can map S3 to it
QUEUE_ARN=$(podman-compose exec ministack aws --endpoint-url http://localhost:4566 sqs get-queue-attributes \
    --queue-url http://127.0.0.1:4566/000000000000/winnow-quarantine-queue \
    --attribute-names QueueArn \
    --query 'Attributes.QueueArn' \
    --output text | tr -d '\r')
echo "Extracted Queue ARN: $QUEUE_ARN"

echo "Creating S3 Buckets in Ministack..."
podman-compose exec ministack aws --endpoint-url http://localhost:4566 s3api create-bucket --bucket winnow-quarantine
podman-compose exec ministack aws --endpoint-url http://localhost:4566 s3api create-bucket --bucket winnow-clean
echo "Buckets created!"

echo "Configuring Native S3 to SQS Event Notifications..."

# Create the notification configuration payload pointing to our SQS ARN
JSON_PAYLOAD="{\"QueueConfigurations\":[{\"QueueArn\":\"$QUEUE_ARN\",\"Events\":[\"s3:ObjectCreated:*\"]}]}"

# Use aws convention for applying the configuration to the local container
podman-compose exec ministack aws --endpoint-url http://localhost:4566 s3api put-bucket-notification-configuration \
    --bucket winnow-quarantine \
    --notification-configuration "$JSON_PAYLOAD"

echo "Setup complete! Ministack S3 put events will now flow directly natively to to the Ministack SQS queue."
