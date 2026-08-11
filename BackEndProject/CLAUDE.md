# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Rules

### General
- **Mobile context**: FE được truy cập chủ yếu qua iPhone 12+ (390px). Khi thiết kế API response — tránh payload thừa, pagination hợp lý, error message rõ ràng để FE hiển thị được trên màn hình nhỏ.
- **Tách commit BE và FE**: KHÔNG gộp code BE (`BackEndProject/`, `Pipeline/`) và FE (`FrontEnd/`) trong cùng 1 commit khi implement feature — tách thành commit riêng cho mỗi bên. **Ngoại lệ**: fix bug thì được gộp BE+FE trong 1 commit.
- **MinIO object key cho SystemImage**: LUÔN dùng `pic.ObjectKey` (computed property trên entity `SystemImage`) khi gọi `IStorageService.GetPresignedUrlAsync`. KHÔNG tự ghép path thủ công, KHÔNG dùng `pic.Name` trực tiếp. `ObjectKey` trả về `{SpecCode}/{Id}{ext}` — đây là nguồn sự thật duy nhất cho đường dẫn ảnh trên MinIO.

### v2.0 AI Stack (Groq + VM3)
- **Groq API key**: `appsettings.Docker.json` chứa `ExternalServices:GroqApi:ApiKey`. Local dev dùng dummy value; Docker prod dùng env var `GROQ_API_KEY` từ docker-compose.
- **Rate limit monitoring**: Mỗi LLM request log timestamp + token count tới Grafana. Alert nếu >25 RPM (buffer dưới 30 RPM limit). Groq trả 429 → return 503 Temporarily Unavailable tới FE (không retry tự động).
- **VM3 embedding service** (:8000): AquaHome BFF gọi qua gateway `/embeddings/embed`. Nếu service down → return 503 (không fallback tới local embedding). Embedding responses cache 1 giờ (key: MD5 của input text).
- **VM3 image search service** (:8001): FishDex API gọi qua gateway `/image-search/*`. Nếu service down → return 503. CLIP embeddings pre-computed trên species_media; image upload queries live (không cache).
- **pgvector constraints**: `SpeciesChunk.Embedding` (384-d, HNSW index) dùng cho RAG. `SpeciesMedia.ClipEmbedding` (512-d, HNSW index) dùng cho image search. Không mix cả hai trong một query — separate code paths.
- **Fallback strategy**: Nếu Groq API down >5min → log alert, fallback command để switch tới Ollama Gemma 2B (Story 2.3 fallback branch, keep ready). FE shows "Powered by local LLM" message.
- **Response latency budgets**:
  - RAG (Groq): <1.5s p95 (embedding 50ms + pgvector 100ms + Groq 1000ms + network 200ms)
  - Image search: <700ms p95 (CLIP 200ms + pgvector 100ms + presigned URLs 300ms + network 100ms)

## Project Overview

FishLover is a microservices-based backend for a fish species management and aquarium tracking platform. It targets .NET 9 / C# 13 and contains three services behind an Ocelot API Gateway.

## Commands

Run .NET commands from `BackEndProject/` (solution root).
Run Docker commands from repo root `D:\Workspace\Practice\FishDex\` — Pipeline/ đã được chuyển ra ngang hàng với BackEndProject/.

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
# UserManagement (PostgreSQL — port 5435)
dotnet ef migrations add <MigrationName> --project UserManagement/UserManagement.EFCore --startup-project UserManagement/UserManagement.API
dotnet ef database update --project UserManagement/UserManagement.EFCore --startup-project UserManagement/UserManagement.API

# AquaHome (PostgreSQL — port 5434)
dotnet ef migrations add <MigrationName> --project AquaHome/AquaHome.EFCore --startup-project AquaHome/AquaHome.API
dotnet ef database update --project AquaHome/AquaHome.EFCore --startup-project AquaHome/AquaHome.API

# FishDex (PostgreSQL — port 5433)
# LƯU Ý: bắt buộc có --startup-project, nếu thiếu sẽ lỗi
# "Unable to resolve service for type 'DbContextOptions<FishDexDbContext>'"
# vì FishDex.EFCore không có design-time factory riêng.
dotnet ef migrations add <MigrationName> --project FishDex/FishDex.EFCore --startup-project FishDex/FishDex.API
dotnet ef database update --project FishDex/FishDex.EFCore --startup-project FishDex/FishDex.API
```

**Local dev — start infrastructure via Docker:**
```
cd Pipeline/local/FishDex        && docker compose up -d   # PostgreSQL 5433
cd Pipeline/local/UserManagement && docker compose up -d   # PostgreSQL 5435, Redis 6379
cd Pipeline/local/AquaHome       && docker compose up -d   # PostgreSQL 5434, Redis 6380
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
../Pipeline/         – Docker Compose stacks + CI/CD pipeline templates (repo root)
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
| DB (UserManagement) | SQL Server (EF Core SqlServer provider) |
| DB (AquaHome) | PostgreSQL 16 (Npgsql EF provider) |
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

Xem `../Pipeline/README.md` để biết cách chạy local Docker stacks và CI/CD templates.

## Production Deployment Checklist

Các shortcuts được cố ý để lại cho local dev — **phải đổi trước khi lên PROD**:

| # | File | Vấn đề | Fix |
|---|------|---------|-----|
| 1 | `UserManagement.API/Extensions/OpenIddictServerExtensions.cs` | `AddEphemeralEncryptionKey()` + `AddEphemeralSigningKey()` — key mất sau restart, multi-instance sẽ lỗi | Dùng X.509 cert từ Key Vault: `AddEncryptionCertificate()` + `AddSigningCertificate()` |
| 2 | Same file | `DisableTransportSecurityRequirement()` — cho phép HTTP | Xóa dòng này; PROD phải chạy HTTPS |
| 3 | `UserManagement.API/Program.cs` | `AutoMigrate:OnStartup` — auto-migrate khi startup, race condition nếu multi-instance | Tắt flag, chạy `dotnet ef database update` như một bước riêng trong CI/CD trước deploy |
| 4 | `FishDex.API/Program.cs` | `ValidateAudience = false` trong scheme "OpenIddict" | Đăng ký FishDex như resource server trong OpenIddict, validate audience đúng |
| 5 | `UserManagement.API/appsettings.Docker.json` | `OpenIddict:Issuer = http://localhost:8080` | Đổi thành HTTPS domain thật của production |
| 6 | Docker Compose | DataProtection Keys không được persist ngoài container | Mount volume hoặc dùng Azure Key Vault / AWS KMS |
