#!/bin/bash

# Exit if required API key is not set
if [ -z "$WINNOW_API_KEY" ]; then
    echo "ERROR: WINNOW_API_KEY environment variable is not set."
    echo "Please set WINNOW_API_KEY before running this script."
    echo "Example: export WINNOW_API_KEY='your-secret-api-key'"
    exit 1
fi

if [ -z "$WINNOW_TENANT_ID" ]; then
    echo "ERROR: WINNOW_TENANT_ID environment variable is not set."
    echo "Please set WINNOW_TENANT_ID before running this script."
    echo "Example: export WINNOW_TENANT_ID='Tenant-A'"
    exit 1
fi

API_URL="${WINNOW_API_URL:-http://localhost:5294}"

echo "Posting error report to Winnow..."
curl -X POST "$API_URL/api/reports" \
  -H "Content-Type: application/json" \
  -H "X-Winnow-Key: $WINNOW_API_KEY" \
  -H "X-Tenant-ID: $WINNOW_TENANT_ID" \
  -d '{
    "message": "Critical Payment Failure",
    "stackTrace": "System.TimeoutException: The operation has timed out.\n   at Winnow.Payments.Gateway.ProcessAsync(PaymentRequest req)\n   at Winnow.Controllers.CheckoutController.SubmitOrder(OrderDto order)",
    "metadata": {
        "userId": "user_123",
        "browser": "Chrome 120.0"
    }
}'
echo -e "\nReport submitted!"