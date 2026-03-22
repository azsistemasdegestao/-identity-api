# Identity API

A generic authentication and identity microservice built with **.NET 8 / ASP.NET Core**, following **Clean Architecture** and **Domain-Driven Design (DDD)** principles.

---

## Features

- JWT access + refresh token authentication
- Role-based authorization (RBAC)
- User registration with welcome email
- Password reset via async email (MassTransit)
- Azure Service Bus support (falls back to InMemory)
- Dockerized, AKS-ready deployment
- Application Insights telemetry
- Swagger / OpenAPI documentation
- Global exception handling and request logging

---

## Tech Stack

| Layer          | Technology                                      |
|----------------|-------------------------------------------------|
| Runtime        | .NET 8 / ASP.NET Core                          |
| Architecture   | Clean Architecture + DDD                        |
| Auth           | ASP.NET Core Identity + JWT Bearer              |
| ORM            | Entity Framework Core 8 + SQL Server           |
| Messaging      | MassTransit (Azure Service Bus / InMemory)      |
| Observability  | Application Insights, structured logging        |
| Docs           | Swashbuckle / OpenAPI                           |
| Container      | Docker (multi-stage Linux build)                |
| Orchestration  | Kubernetes (AKS)                                |
| CI/CD          | GitHub Actions                                  |

---

## Quick Start

```bash
# 1. Clone the repository
git clone https://github.com/azsistemasdegestao/-identity-api.git
cd -identity-api

# 2. Restore dependencies
dotnet restore identity-api.sln

# 3. Apply EF Core migrations (requires a running SQL Server)
dotnet ef database update --project src/IdentityApi.Infrastructure --startup-project src/IdentityApi.WebApi

# 4. Run the API
dotnet run --project src/IdentityApi.WebApi

# 5. Open Swagger UI
# http://localhost:5100/swagger
```

> Configure `src/IdentityApi.WebApi/appsettings.Development.json` with your local SQL Server connection string before running.

---

## API Endpoints

### Auth — `api/auth` (public)

| Method | Endpoint              | Description                           |
|--------|-----------------------|---------------------------------------|
| POST   | `/register`           | Register a new user                   |
| POST   | `/login`              | Login and receive JWT tokens          |
| POST   | `/refresh-token`      | Refresh access token                  |
| POST   | `/forgot-password`    | Request password reset email          |
| POST   | `/reset-password`     | Reset password using token from email |

### Users — `api/users` (requires Bearer token)

| Method | Endpoint              | Required Role | Description              |
|--------|-----------------------|---------------|--------------------------|
| GET    | `/{id:guid}`          | Any           | Get user by ID           |
| POST   | `/assign-role`        | Admin         | Assign a role to a user  |

---

## Architecture Diagram

```
┌─────────────────────────────────────────┐
│              IdentityApi.WebApi          │
│  (Controllers, Filters, Program.cs)      │
└────────────────┬────────────────────────┘
                 │ depends on
┌────────────────▼────────────────────────┐
│           IdentityApi.Application        │
│  (DTOs, Service Interfaces)              │
└────────────────┬────────────────────────┘
                 │ depends on
┌────────────────▼────────────────────────┐
│            IdentityApi.Domain            │
│  (Entities, Value Objects, Interfaces)   │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│         IdentityApi.Infrastructure       │
│  (EF Core, Identity, JWT, MassTransit)   │
│  implements Domain + Application interfaces│
└─────────────────────────────────────────┘
```

The `Infrastructure` layer implements interfaces defined in `Domain` and `Application`. It depends on both, while `Domain` has zero external dependencies.

---

## Deployment

### Docker

```bash
docker build -t identity-api .
docker run -p 8080:8080 identity-api
```

### Kubernetes (AKS)

Manifests are located in the `/k8s` directory:

- `namespace.yml` — Kubernetes namespace
- `configmap.yml` — Application configuration with placeholder substitution
- `deployment.yml` — 2-replica deployment with health probes
- `service.yml` — LoadBalancer service (port 80 → 8080)

### CI/CD

The GitHub Actions workflow (`.github/workflows/deploy-aks.yml`) runs on push to `main`:

1. **test** — Restore, build, and run tests with code coverage
2. **build-and-push** — Build Docker image and push to ACR
3. **deploy** — Substitute secrets into configmap, apply Kubernetes manifests, roll out deployment

#### Required GitHub Secrets

| Secret                        | Description                          |
|-------------------------------|--------------------------------------|
| `ACR_REGISTRY`                | Azure Container Registry hostname    |
| `ACR_USERNAME`                | ACR username                         |
| `ACR_PASSWORD`                | ACR password                         |
| `AKS_CLUSTER_NAME`            | AKS cluster name                     |
| `AKS_RESOURCE_GROUP`          | AKS resource group                   |
| `AZURE_CREDENTIALS`           | Azure service principal JSON         |
| `SQL_CONNECTION_STRING`       | Production SQL Server connection     |
| `JWT_ISSUER`                  | JWT issuer URL                       |
| `JWT_AUDIENCE`                | JWT audience URL                     |
| `JWT_KEY_ACCESS`              | Access token signing key (min 32 ch) |
| `JWT_KEY_REFRESH`             | Refresh token signing key (min 32 ch)|
| `SERVICE_BUS_CONNECTION_STRING` | Azure Service Bus connection       |
| `PASSWORD_RESET_URL`          | Frontend password reset URL          |
| `APP_INSIGHTS_CONNECTION_STRING` | Application Insights connection   |

---

## License

MIT
