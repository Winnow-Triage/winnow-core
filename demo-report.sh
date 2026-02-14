#!/bin/bash
echo "Posting error report to Winnow..."
curl -X POST http://localhost:5294/api/reports \
  -H "Content-Type: application/json" \
  -H "X-Winnow-Key: secret-key" \
  -H "X-Tenant-ID: Tenant-A" \
  -d '{
    "message": "Critical Payment Failure",
    "stackTrace": "System.TimeoutException: The operation has timed out.\n   at Winnow.Payments.Gateway.ProcessAsync(PaymentRequest req)\n   at Winnow.Controllers.CheckoutController.SubmitOrder(OrderDto order)",
    "metadata": {
        "userId": "user_123",
        "browser": "Chrome 120.0"
    }
}'
echo -e "\nReport submitted!"
