# Winnow Bouncer Autoscaling via KEDA & SQS Setup Guide

To support 0-to-N scaling for the Bouncer worker, we rely on KEDA (Kubernetes Event-driven Autoscaling). We use MinIO locally and standard S3 in production. MinIO can natively publish bucket notifications to an SQS queue (which we stub out via LocalStack locally). 

## 1. Local Infrastructure Assumptions
- MinIO is running (e.g. `http://localhost:9000`)
- LocalStack is running SQS (e.g. `http://localhost:4566`)
- Kubernetes cluster (k3d/Minikube) has KEDA installed.

## 2. Setting up the Local Queue & MinIO (Automated Script)
We have provided a zero-dependency script that uses `docker` or `podman` to spin up ephemeral containers containing the AWS and MinIO CLIs to configure the environment for you.

Simply run:
```bash
./k8s/setup-local-env.sh
```

This script will:
1. Create the `winnow-quarantine-queue` in LocalStack.
2. Configure MinIO (on `localhost:9000`) to send webhook events to LocalStack.
3. Add the `put` event notification to the `winnow-quarantine` bucket.

## 4. Deploying Bouncer
Make sure the `ScaledObject` and `Deployment` manifests are applied to your namespace:

```bash
kubectl apply -f k8s/bouncer-deployment.yaml
```

The `ScaledObject` specifies `minReplicaCount: 0` and polls every 5 seconds. If a file lands in the bucket, MinIO drops a notification into the SQS queue. KEDA reads the queue depth and spins up Bouncer pods to handle the load.
