# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

FishLover is a microservices-based backend for a fish species management and aquarium tracking platform. It targets .NET 9 / C# 13 and contains three services behind an Ocelot API Gateway.

## Commands

Run all commands from `BackEndProject/` (solution root).

**Build the solution:**
```
dotnet build FishDex.sln
```

**Run individual services:**
```
dotnet run --project UserManagement/UserManagement.API/UserManagement.API.csproj
dotnet run --project FishDex/FishDex.API/FishDex.API.csproj
dotnet run --project ApiGateway/ApiGateway.csproj
```

**Run with hot reload:**
```
dotnet watch --project UserManagement/UserManagement.API/UserManagement.API.csproj
```

**EF Core migrations:**
```
# UserManagement (SQL Server)
dotnet ef migrations add <MigrationName> --project UserManagement/UserManagement.EFCore
dotnet ef database update --project UserManagement/UserManagement.EFCore

# FishDex (PostgreSQL)
dotnet ef migrations add <MigrationName> --project FishDex/FishDex.EFCore
dotnet ef database update --project FishDex/FishDex.EFCore
```

**Local dev — start infrastructure via Docker:**
```
cd Pipeline/FishDexLocal       && docker compose up -d   # PostgreSQL 5433
cd Pipeline/UserManagementLocal && docker compose up -d  # SQL Server 1433, Redis 6379
cd Pipeline/AquaHomeLocal       && docker compose up -d  # SQL Server 1434, Redis 6380
```

**Default service ports:**
- ApiGateway → `5000`
- UserManagement.API → `8080`
- FishDex.API → (TBD)

## Architecture

### Service Layout

```
ApiGateway/          – Ocelot gateway; routes /api/** → downstream services
UserManagement/      – User auth/management microservice (SQL Server + Redis)
FishDex/             – Fish species data service (PostgreSQL + pgvector)
AquaHome/            – Aquarium tracking service (SQL Server + Redis) [in progress]
Share/               – Cross-service shared library (JWT, OpenTelemetry, CurrentUserSession)
Pipeline/            – Docker Compose stacks + CI/CD pipeline templates
```

### UserManagement Layers

The service follows a strict three-layer split:

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
- `PagedResult<T>` — generic pagination wrapper shared across all services

### API Gateway Routing (`ApiGateway/ocelot.json`)

- `/api/{everything}` → `user-management:8080`
- `/storage/{everything}` → `storage:9000` (MinIO or equivalent object store)

### Authentication Flow

JWT Bearer tokens; configuration in `appsettings.json` under `JwtSettings` (SecretKey, Issuer, Audience, ExpiryMinutes). Invitation-based registration is gated by `RequireInvitation: true` in config. Token refresh is supported. Email verification and password-reset flows use **Resend** as the email provider; templates live in `UserManagement.Domain/Helper/`.

### FishDex Data Model

`FishDex/FishDex.EFCore/Entity/` is organized by subdomain: `Species/`, `Ecologies/`, `Ecosystem/`, `MorphData/`, `Occurrence/`, `Stocks/`, `Media/`. Uses PostgreSQL 16 with pgvector extension.

### Key Libraries

| Concern | Library |
|---|---|
| ORM | Entity Framework Core 9 |
| DB (UserManagement / AquaHome) | SQL Server (EF Core SqlServer provider) |
| DB (FishDex) | PostgreSQL 16 + pgvector (Npgsql EF provider) |
| Gateway | Ocelot 24 |
| DI | Autofac 9 |
| Mapping | Static `ToDto()` extension methods (no AutoMapper) |
| Validation | FluentValidation 11 |
| Logging | Serilog (file rotation) |
| Tracing/Metrics | OpenTelemetry 1.10 + Prometheus |
| Caching | StackExchange.Redis 2.8 |
| Auth | ASP.NET Core Identity + JWT Bearer |

## NuGet

Project-level `nuget.config` forces restore từ `nuget.org` only. Không dùng private feed trong repo này.

## Configuration Notes

- `appsettings.Development.json` enables test data seeding and debug logging; use it locally.
- `appsettings.Docker.json` is the Docker-optimized profile.
- Initial admin credentials are supplied via environment variables at first run.

## Pipeline

Xem `Pipeline/README.md` để biết cách chạy local Docker stacks và CI/CD templates.
