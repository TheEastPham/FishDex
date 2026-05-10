# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

FishLover is a microservices-based backend for a fish species management and aquarium tracking platform. It targets .NET 9 / C# 13 and currently contains two services behind an Ocelot API Gateway.

## Commands

**Build the solution:**
```
dotnet build UserManagement/FishDex.sln
```

**Run individual services:**
```
dotnet run --project UserManagement/UserManagement.API/UserManagement.API.csproj
dotnet run --project ApiGateway/ApiGateway.csproj
```

**Run with hot reload:**
```
dotnet watch --project UserManagement/UserManagement.API/UserManagement.API.csproj
```

**EF Core migrations (run from solution root):**
```
dotnet ef migrations add <MigrationName> --project UserManagement/UserManagement.EFCore
dotnet ef database update --project UserManagement/UserManagement.EFCore
```

**Local dev prerequisites:**
- SQL Server LocalDB (`(localdb)\mssqllocaldb`) — database `baseUserManagement`
- Redis on `localhost:6379`
- ApiGateway → port 5000; UserManagement.API → port 8080

## Architecture

### Service Layout

```
ApiGateway/          – Ocelot gateway; routes /api/** → user-management:8080
UserManagement/      – User auth/management microservice (FishDex.sln)
FishDex/             – Fish species data service (EFCore layer only, no API yet)
Share/               – Cross-service shared library (JWT, OpenTelemetry, CurrentUserSession)
```

### UserManagement Layers

The service follows a strict three-layer split inside the solution:

| Project | Role |
|---|---|
| `UserManagement.API` | Controllers, middleware, DI bootstrap, Serilog/OpenTelemetry wiring |
| `UserManagement.Domain` | Business logic, DTOs, service interfaces, email templates (EN/VI) |
| `UserManagement.EFCore` | EF Core DbContext, entities, migrations, repository implementations |

DI is wired via **Autofac modules** (`UserManagementModule` in Domain, a matching module in EFCore). Controllers depend only on domain service interfaces; the EFCore module binds the concrete repositories at startup.

### Cross-Cutting Concerns (Share/FishLover.Shared)

- `JwtAuthenticationExtensions` / `AuthorizationExtensions` — shared JWT setup consumed by every API service
- `CurrentUserSession` / `ICurrentUserSession` — scoped service that exposes the authenticated user's claims to domain services
- `OpenTelemetryExtensions` — wires traces + Prometheus metrics; export endpoint is configured via `appsettings.json`

### API Gateway Routing (`ApiGateway/ocelot.json`)

- `/api/{everything}` → `user-management:8080`
- `/storage/{everything}` → `storage:9000` (MinIO or equivalent object store)

### Authentication Flow

JWT Bearer tokens; configuration in `appsettings.json` under `JwtSettings` (SecretKey, Issuer, Audience, ExpiryMinutes). Invitation-based registration is gated by `RequireInvitation: true` in config. Token refresh is supported. Email verification and password-reset flows use **Resend** as the email provider; templates live in `UserManagement.Domain/Helper/`.

### FishDex Data Model

`FishDex/FishDex.EFCore/Entity/` is organized by subdomain: `Species/`, `Ecologies/`, `Ecosystem/`, `MorphData/`, `Occurrence/`, `Stocks/`, `Media/`. No API layer exists yet for this service.

### Key Libraries

| Concern | Library |
|---|---|
| ORM | Entity Framework Core 9 (SQL Server) |
| Gateway | Ocelot 24 |
| DI | Autofac 9 |
| Mapping | AutoMapper 13 |
| Validation | FluentValidation 11 |
| Logging | Serilog (file rotation) |
| Tracing/Metrics | OpenTelemetry 1.10 + Prometheus |
| Caching | StackExchange.Redis 2.8 |
| Auth | ASP.NET Core Identity + JWT Bearer |

## Configuration Notes

- `appsettings.Development.json` enables test data seeding and debug logging; use it locally.
- `appsettings.Docker.json` is the Docker-optimized profile.
- Initial admin credentials are supplied via environment variables at first run.