# SmartPPC Deployment Guide

This guide covers deploying SmartPPC using Kubernetes on Docker Desktop.

## Prerequisites

- **Docker Desktop** installed and running
- **Kubernetes enabled** in Docker Desktop
- At least **4GB RAM** available for containers
- Ports **30080** and **30050** available

---

## Folder Structure

```
deploy/
├── k8s/                      # Kubernetes manifests (primary)
│   ├── namespace.yaml        # SmartPPC namespace
│   ├── secrets.yaml          # Secrets and ConfigMap
│   ├── postgres.yaml         # PostgreSQL StatefulSet
│   ├── api.yaml              # SmartPPC API Deployment
│   └── pgadmin.yaml          # pgAdmin (optional)
├── docker-compose/           # Docker Compose (alternative)
│   ├── docker-compose.prod.yml
│   └── .env.template
└── DEPLOYMENT.md             # This file
```

---

## Kubernetes Deployment (Primary)

### Step 1: Enable Kubernetes in Docker Desktop

1. Open **Docker Desktop**
2. Click **⚙️ Settings** (gear icon)
3. Go to **Kubernetes** in the left sidebar
4. ✅ Check **"Enable Kubernetes"**
5. Click **"Apply & Restart"**
6. Wait 2-5 minutes for Kubernetes to start
7. Verify with green "Kubernetes running" indicator

### Step 2: Verify Kubernetes is Running

```powershell
kubectl cluster-info
kubectl get nodes
```

You should see output indicating the cluster is running.

### Step 3: Configure Secrets

Before deploying, edit the secrets file with your passwords:

```powershell
# Open and edit the secrets file
notepad deploy/k8s/secrets.yaml
```

Change these values in `secrets.yaml`:
- `postgres-password`: Your database password
- `pgadmin-password`: Your pgAdmin password

### Step 4: Build the Docker Image

```powershell
# Navigate to project root
cd c:\Repos\DDMRP\SmartPPC

# Build the API image
docker build -t smartppc-api:latest -f SmartPPC.Api/Dockerfile .
```

### Step 5: Deploy to Kubernetes

```powershell
# Apply manifests in order
kubectl apply -f deploy/k8s/namespace.yaml
kubectl apply -f deploy/k8s/secrets.yaml
kubectl apply -f deploy/k8s/postgres.yaml

# Wait for PostgreSQL to be ready
kubectl wait --for=condition=ready pod -l app=postgres -n smartppc --timeout=120s

# Deploy the API
kubectl apply -f deploy/k8s/api.yaml

# (Optional) Deploy pgAdmin
kubectl apply -f deploy/k8s/pgadmin.yaml
```

### Step 6: Verify Deployment

```powershell
# Check all resources
kubectl get all -n smartppc

# Check pod status (wait for Running state)
kubectl get pods -n smartppc -w
```

---

## Access the Application

| Service | URL | Description |
|---------|-----|-------------|
| **SmartPPC App** | http://localhost:30080 | Main application |
| **pgAdmin** | http://localhost:30050 | Database management |

---

## Kubernetes Commands Reference

### View Status

```powershell
# Get all resources in namespace
kubectl get all -n smartppc

# Get pods with details
kubectl get pods -n smartppc -o wide

# Get services
kubectl get svc -n smartppc
```

### View Logs

```powershell
# API logs
kubectl logs -f deployment/smartppc-api -n smartppc

# PostgreSQL logs
kubectl logs -f statefulset/postgres -n smartppc

# Logs from specific pod
kubectl logs -f <pod-name> -n smartppc
```

### Troubleshooting

```powershell
# Describe pod (shows events and errors)
kubectl describe pod -l app=smartppc-api -n smartppc

# Get pod events
kubectl get events -n smartppc --sort-by='.lastTimestamp'

# Execute shell in pod
kubectl exec -it deployment/smartppc-api -n smartppc -- /bin/bash

# Test database connection from API pod
kubectl exec -it deployment/smartppc-api -n smartppc -- curl -v postgres:5432
```

### Restart Services

```powershell
# Restart API deployment
kubectl rollout restart deployment/smartppc-api -n smartppc

# Restart PostgreSQL (careful - may cause brief downtime)
kubectl rollout restart statefulset/postgres -n smartppc
```

### Update After Code Changes

```powershell
# Rebuild the Docker image
docker build -t smartppc-api:latest -f SmartPPC.Api/Dockerfile .

# Restart deployment to pick up new image
kubectl rollout restart deployment/smartppc-api -n smartppc

# Watch rollout status
kubectl rollout status deployment/smartppc-api -n smartppc
```

### Clean Up

```powershell
# Delete everything (keeps persistent volumes)
kubectl delete namespace smartppc

# Delete specific resource
kubectl delete deployment smartppc-api -n smartppc

# Delete persistent volume claims (DELETES DATA!)
kubectl delete pvc -n smartppc --all
```

---

## Configuration

### Kubernetes Secrets

Secrets are stored in [k8s/secrets.yaml](k8s/secrets.yaml):

| Secret | Description |
|--------|-------------|
| `postgres-password` | PostgreSQL database password |
| `pgadmin-password` | pgAdmin login password |

### ConfigMap Values

| Key | Default | Description |
|-----|---------|-------------|
| `POSTGRES_DB` | smartppc | Database name |
| `POSTGRES_USER` | smartppc_user | Database username |
| `ASPNETCORE_ENVIRONMENT` | Production | ASP.NET environment |
| `PGADMIN_EMAIL` | admin@smartppc.local | pgAdmin login email |

### Update Configuration

```powershell
# Edit secrets or configmap
notepad deploy/k8s/secrets.yaml

# Apply changes
kubectl apply -f deploy/k8s/secrets.yaml

# Restart deployments to pick up changes
kubectl rollout restart deployment/smartppc-api -n smartppc
```

---

## Backup and Restore

### Backup PostgreSQL

```powershell
# Create backup
kubectl exec -n smartppc postgres-0 -- pg_dump -U smartppc_user smartppc > backup_$(Get-Date -Format "yyyyMMdd").sql
```

### Restore PostgreSQL

```powershell
# Restore from backup
Get-Content backup.sql | kubectl exec -i -n smartppc postgres-0 -- psql -U smartppc_user -d smartppc
```

---

## Network Access from Other Devices

To access SmartPPC from other devices on your home network:

1. Find your PC's local IP:
   ```powershell
   ipconfig
   # Look for IPv4 Address (e.g., 192.168.1.100)
   ```

2. Access the app at: `http://<YOUR-PC-IP>:30080`

3. Allow port through Windows Firewall (run as Administrator):
   ```powershell
   New-NetFirewallRule -DisplayName "SmartPPC K8s HTTP" -Direction Inbound -Port 30080 -Protocol TCP -Action Allow
   ```

---

## Connecting pgAdmin to PostgreSQL

1. Open http://localhost:30050
2. Login with credentials from `secrets.yaml`
3. Right-click "Servers" → "Register" → "Server"
4. **General** tab: Name = `SmartPPC`
5. **Connection** tab:
   - Host: `postgres.smartppc.svc.cluster.local`
   - Port: `5432`
   - Username: `smartppc_user`
   - Password: (from secrets.yaml)

---

## Troubleshooting Common Issues

### Pods stuck in "Pending" state

```powershell
# Check events for details
kubectl describe pod -l app=smartppc-api -n smartppc

# Common causes:
# - Insufficient resources: Check Docker Desktop memory settings
# - PVC not bound: Check storage provisioner
```

### ImagePullBackOff error

```powershell
# Ensure image exists locally
docker images | findstr smartppc

# Rebuild if needed
docker build -t smartppc-api:latest -f SmartPPC.Api/Dockerfile .
```

### Database connection failed

```powershell
# Check if PostgreSQL is running
kubectl get pods -l app=postgres -n smartppc

# Check PostgreSQL logs
kubectl logs -f statefulset/postgres -n smartppc

# Verify service exists
kubectl get svc postgres -n smartppc
```

### Reset Everything

```powershell
# Delete namespace (removes all resources)
kubectl delete namespace smartppc

# Re-deploy from scratch
kubectl apply -f deploy/k8s/namespace.yaml
kubectl apply -f deploy/k8s/secrets.yaml
kubectl apply -f deploy/k8s/postgres.yaml
kubectl wait --for=condition=ready pod -l app=postgres -n smartppc --timeout=120s
kubectl apply -f deploy/k8s/api.yaml
kubectl apply -f deploy/k8s/pgadmin.yaml
```

---

## Alternative: Docker Compose

If you prefer a simpler deployment without Kubernetes, use Docker Compose files in `deploy/docker-compose/`:

```powershell
cd c:\Repos\DDMRP\SmartPPC

# Copy and configure environment
Copy-Item deploy/docker-compose/.env.template deploy/docker-compose/.env
notepad deploy/docker-compose/.env

# Start services
docker compose -f deploy/docker-compose/docker-compose.prod.yml up -d --build

# Access at http://localhost:8080
```

See [docker-compose/](docker-compose/) folder for the Docker Compose configuration files.
