# Kong DevOps Portal

Internal ASP.NET Core MVC portal for managing Kong Gateway Community Edition.

## Features
- Full consumer management (CRUD + extended profiles + contacts)
- All auth types: key-auth, jwt, basic-auth, hmac-auth, oauth2
- Bulk credential rotation with grace period + confirmation flow
- Service, Route, Plugin management
- Role-based access: Admin / Operator / Viewer
- Full audit log
- SQL Server backend

## Stack
- ASP.NET Core 8 MVC
- Entity Framework Core + SQL Server
- Docker + Kubernetes
- Kong Admin API

## Setup

### 1. Configure secrets (never in code)
```bash
kubectl create secret generic kong-portal-secrets \
  --from-literal=DB_CONNECTION='Server=...;Database=KongPortal;...' \
  --from-literal=KONG_ADMIN_URL='http://kong-admin:8001' \
  -n kong-portal
```

### 2. Run locally
```bash
# Set env vars
export ConnectionStrings__DefaultConnection="Server=localhost;Database=KongPortal;..."
export Kong__AdminUrl="http://localhost:8001"

cd src/KongPortal
dotnet run
```

### 3. Docker
```bash
docker build -f docker/Dockerfile -t kong-portal .
docker run -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="..." \
  -e Kong__AdminUrl="http://kong-admin:8001" \
  kong-portal
```

### 4. Kubernetes
```bash
kubectl apply -f k8s/manifests.yaml
```

## Roles
| Role | Can Do |
|---|---|
| Viewer | Read only |
| Operator | Rotate, manage consumers |
| Admin | Full access including delete |

## Rotation Flow
1. Select consumers → Rotate
2. New credential sent to contact email
3. Consumer confirms new key works
4. Mark confirmed in portal
5. Delete old credentials

## Security Notes
- Kong Admin API must be ClusterIP only
- Portal accessible via VPN/internal network only
- Secrets via K8s Secrets only — never in config files
- Audit log is append-only
