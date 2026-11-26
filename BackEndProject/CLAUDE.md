# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Rules

### General
- **Mobile context**: FE được truy cập chủ yếu qua iPhone 12+ (390px). Khi thiết kế API response — tránh payload thừa, pagination hợp lý, error message rõ ràng để FE hiển thị được trên màn hình nhỏ.
- **Tách commit BE và FE**: KHÔNG gộp code BE (`BackEndProject/`, `Pipeline/`) và FE (`FrontEnd/`) trong cùng 1 commit khi implement feature — tách thành commit riêng cho mỗi bên. **Ngoại lệ**: fix bug thì được gộp BE+FE trong 1 commit.
- **Object key cho SystemImage**: LUÔN dùng `pic.ObjectKey` (computed property trên entity `SystemImage`) khi gọi `IStorageService.GetPresignedUrlAsync`. KHÔNG tự ghép path thủ công, KHÔNG dùng `pic.Name` trực tiếp. `ObjectKey` trả về `{SpecCode}/{Id}{ext}` — đây là nguồn sự thật duy nhất cho đường dẫn ảnh trên storage (Cloudflare R2, bucket `system-image`).
- **Object key policy**: KHÔNG tự ghép key storage ở service. FishDex: ảnh FishBase dùng `SystemImage.ObjectKey`, key khác dùng `FishDexObjectKeys`. AquaHome: luôn qua `AquaHomeObjectKeys` (inject). Key đã lưu trong DB — test `AquaHome.Tests`/`FishDexObjectKeysTests` khoá từng format, đổi format = mất object cũ.
- **Upload qua presigned PUT không tự chặn dung lượng**: luồng nào phát URL PUT phải gọi `IStorageService.GetObjectSizeAsync` (HEAD) ở bước confirm.

### v2.0 AI Stack (Groq + VM3) — 🚧 CHƯA XÂY
Chưa có dòng code nào cho phần này (chưa có `SpeciesChunk`, route `/embeddings`, `/image-search`, cấu hình Groq). Các mục dưới là **ràng buộc thiết kế** phải giữ khi bắt đầu làm R0–R4; nguồn chính là `../docs/roadmap/ai-release-plan.md` — lệch nhau thì theo release plan.
- **Groq API key**: lấy từ env var `GROQ_API_KEY` (prod) / giá trị dummy khi dev; không commit key.
- **Rate limit monitoring**: Mỗi LLM request log timestamp + token count tới Grafana. Alert nếu >25 RPM (buffer dưới 30 RPM limit). Groq trả 429 → return 503 Temporarily Unavailable tới FE (không retry tự động).
- **VM3 embedding service** (:8000): AquaHome gọi qua gateway (route sẽ thêm khi làm R0). Nếu service down → return 503 (không fallback tới local embedding). Embedding responses cache 1 giờ (key: MD5 của input text).
- **VM3 image search service** (:8001): FishDex API gọi qua gateway (route sẽ thêm khi làm R4). Nếu service down → return 503. CLIP embeddings pre-computed trên species media; ảnh upload truy vấn live (không cache).
- **pgvector constraints**: `SpeciesChunk.Embedding` (384-d, HNSW index) dùng cho RAG. `SpeciesMedia.ClipEmbedding` (512-d, HNSW index) dùng cho image search. Không mix cả hai trong một query — separate code paths.
- **Fallback strategy**: LLM đi qua `ILlmProvider` (Groq → Ollama/Gemini) như release plan; Groq lỗi kéo dài thì chuyển provider và FE hiện thông báo đang dùng LLM dự phòng.
- **Response latency budgets**:
  - RAG (Groq): <1.5s p95 (embedding 50ms + pgvector 100ms + Groq 1000ms + network 200ms)
  - Image search: <700ms p95 (CLIP 200ms + pgvector 100ms + presigned URLs 300ms + network 100ms)

## Project Overview

FishLover is a microservices-based backend for a fish species management and aquarium tracking platform. It targets .NET 9 / C# 13 and contains three services (UserManagement, FishDex, AquaHome) plus a background worker (AquaHome.Worker) behind an Ocelot API Gateway.

## Commands

Run .NET commands from `BackEndProject/` (solution root).
Run Docker commands from repo root `D:\Workspace\FishLover\FishDex\` — Pipeline/ nằm ngang hàng với BackEndProject/.
Tài liệu cấp hệ thống (C4, roadmap AI, BE/FE guide; mục lục ở `../docs/README.md`) nằm ở `../docs/`; ADR và GLOSSARY riêng của BE nằm trong `docs/adr/` và `GLOSSARY.md` của thư mục này.

**Build the solution:**
```
dotnet build FishDex.sln
```

**Run individual services:**
```
dotnet run --project UserManagement/UserManagement.API/UserManagement.API.csproj
dotnet run --project FishDex/FishDex.API/FishDex.API.csproj
dotnet run --project AquaHome/AquaHome.API/AquaHome.API.csproj
dotnet run --project AquaHome/AquaHome.Worker/AquaHome.Worker.csproj
dotnet run --project ApiGateway/ApiGateway.csproj
```

**Run tests** (`dotnet test FishDex.sln` chạy cả 5):
```
dotnet test Share/FishLover.Storage.Tests/FishLover.Storage.Tests.csproj   # thuần, không cần Docker
dotnet test Share/FishLover.Shared.Tests/FishLover.Shared.Tests.csproj     # thuần, không cần Docker
dotnet test AquaHome/AquaHome.Tests/AquaHome.Tests.csproj                 # thuần, không cần Docker
dotnet test FishDex/FishDex.Tests/FishDex.Tests.csproj                    # cần Docker — Testcontainers dựng Postgres pgvector/pgvector:pg16
dotnet test UserManagement/UserManagement.Tests/UserManagement.Tests.csproj # cần Docker — dựng cả API (WebApplicationFactory) trên Postgres thật, email giả
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

**Local dev — Docker (một file duy nhất: `Pipeline/local/docker-compose.local.yml`, biến trong `Pipeline/local/.env`):**
```
cd Pipeline/local
docker compose -f docker-compose.local.yml up -d     # toàn bộ stack
docker compose -f docker-compose.local.yml up -d usermanagement-db usermanagement-redis fishdex-db aquahome-db aquahome-redis   # chỉ hạ tầng
```
PostgreSQL: UserManagement 5435 · FishDex 5433 · AquaHome 5434. Redis: UserManagement 6379 · AquaHome 6380.

**Default service ports:**
- ApiGateway → `5000`
- UserManagement.API → `8080`
- FishDex.API → `8081`
- AquaHome.API → `8082`

## Architecture

### Service Layout

```
ApiGateway/          – Ocelot gateway (route: xem "API Gateway Routing" bên dưới)
UserManagement/      – Identity + OpenIddict (OAuth2/OIDC PKCE), đăng ký OTP, Web Push — PostgreSQL + Redis
FishDex/             – Tra cứu loài (FishBase + Community species), market layer, media-access — PostgreSQL + pgvector
AquaHome/            – Bể nuôi, snapshot công khai, contest (YouTube), bài viết, quota — PostgreSQL + Redis
                       AquaHome.Worker: job nền (đăng video contest lên YouTube, dọn staging, sync viewCount, sync market species, nhắc việc) — chỉ một instance
Share/               – FishLover.Shared: JWT/resource-server bootstrap, internal API key auth, OpenTelemetry, CurrentUserSession, PagedResult
                       FishLover.Storage: IStorageService (S3/R2 + in-memory), upload verifier, media-access signer
../Pipeline/         – Docker Compose local + production (OracleVM) + 4 pipeline Azure DevOps (repo root)
```

### UserManagement Layers

The service follows a strict three-layer split:

| Project | Role |
|---|---|
| `UserManagement.API` | Controllers, middleware, DI bootstrap, Serilog/OpenTelemetry wiring |
| `UserManagement.Domain` | Business logic, DTOs, service interfaces, email templates (EN/VI) |
| `UserManagement.EFCore` | EF Core DbContext, entities, migrations, repository implementations |

DI dùng container mặc định của ASP.NET Core qua extension method: `AddUserManagementDomain` (Domain), `AddUserManagementEFCore` (EFCore), `AddOpenIddictServer` (API). Controllers depend only on domain service interfaces. FishDex và AquaHome theo cùng kiểu `ServiceCollectionExtensions`.

### Cross-Cutting Concerns (Share/FishLover.Shared)

- `AddFishLoverJwtAuthentication` / `AddFishLoverAuthorization` — scheme JWT (`JwtSettings`) cho ApiGateway và UserManagement
- `AddFishLoverResourceServer` — scheme "OpenIddict" cho FishDex và AquaHome (validate token do UserManagement cấp; chưa validate audience, xem ADR 0004)
- `AddFishLoverInternalApiKey` — xác thực service-to-service bằng internal API key
- `AddFishLoverTelemetry` / `AddFishLoverWorkerTracing` — traces + Prometheus metrics; endpoint cấu hình trong `appsettings.json`
- `CurrentUserSession` / `ICurrentUserSession` — scoped service that exposes the authenticated user's claims to domain services
- `PagedResult<T>` — generic pagination wrapper shared across all services

### API Gateway Routing

Mỗi môi trường nạp **đúng một** file route: `ocelot.json` (Production), `ocelot.Development.json`, `ocelot.Docker.json`. Không chồng file môi trường lên `ocelot.json` (cấu hình .NET gộp mảng theo vị trí). Thêm route thì thêm vào **cả ba** file.

| Upstream | Downstream (Production) | Ghi chú |
|---|---|---|
| `/.well-known/{everything}`, `/connect/{everything}` | `usermanagement:8080` | OIDC; `/connect` giới hạn 20 req/phút |
| `/user/v1/{everything}` | `usermanagement:8080/api/...` | 30 req/phút |
| `/fishdex/v1/{everything}` | `fishdex:8081/api/...` | 60 req/phút; `/fishdex/v1/public/media-access` (+ `/sas`) là route riêng `Priority 2`, không giới hạn |
| `/aquahome/v1/{everything}` | `aquahome:8082/api/...` | 60 req/phút |

`/metrics` được map **trước** `UseOcelot` (Ocelot là middleware terminal); nginx chặn `/metrics` từ internet.

### Authentication Flow

- **FE đăng nhập bằng OAuth2 Authorization Code + PKCE** qua OpenIddict ở UserManagement (`/connect/*`, discovery `/.well-known/*`); FishDex và AquaHome nhận token qua scheme "OpenIddict".
- `JwtSettings` (SecretKey, Issuer, Audience) phục vụ scheme JWT cũ ở ApiGateway/UserManagement; `/api/auth/login|refresh|logout` của `AuthController` thuộc luồng này, FE không dùng.
- **Đăng ký:** gửi mã OTP qua email rồi `/api/auth/register`; `RequireInvitation` bật/tắt việc bắt buộc mã mời. Quên mật khẩu: `/api/auth/forgot-password` → `reset-password`.
- Email gửi qua **Resend**; template ở `UserManagement.Domain/Helper/EmailTemplates/{en,vi}/` (VerificationCode, PasswordReset, WelcomeEmail).

### FishDex Data Model

`FishDex/FishDex.EFCore/Entity/` is organized by subdomain: `Species/`, `Ecologies/`, `Ecosystem/`, `MorphData/`, `Occurrence/`, `Stocks/`, `Media/`. Uses PostgreSQL 16 with pgvector extension.

### Key Libraries

| Concern | Library |
|---|---|
| ORM | Entity Framework Core 9 |
| DB (cả 3 service) | PostgreSQL 16 (Npgsql EF provider); FishDex có thêm pgvector |
| Gateway | Ocelot 24 |
| DI | Container mặc định của ASP.NET Core (`IServiceCollection` extensions) |
| Mapping | Static `ToDto()` extension methods (no AutoMapper) |
| Validation | DataAnnotations trên DTO + kiểm tra trong service (không dùng FluentValidation) |
| Logging | Serilog (file rotation) |
| Tracing/Metrics | OpenTelemetry 1.10 + Prometheus |
| Caching | Redis (`IDistributedCache`, StackExchange.Redis 2.8) + `IMemoryCache` |
| Auth | ASP.NET Core Identity + OpenIddict 5 + JWT Bearer |
| Storage | AWSSDK.S3 tới Cloudflare R2 (`FishLover.Storage`) |
| Khác | Google.Apis.YouTube.v3 (contest), WebPush (thông báo), Testcontainers.PostgreSql + xUnit (test) |

## NuGet

Project-level `nuget.config` forces restore từ `nuget.org` only. Không dùng private feed trong repo này.

## Configuration Notes

- `appsettings.Development.json` enables test data seeding and debug logging; use it locally.
- `appsettings.Docker.json` is the Docker-optimized profile.
- Initial admin credentials are supplied via environment variables at first run.

## Pipeline

Xem `../Pipeline/README.md` để biết cách chạy local Docker stack và 4 pipeline Azure DevOps (chạy khi push lên `release/v1.2`).

## Production Deployment Checklist

Các shortcut còn lại / đã xử lý (giữ nguyên số thứ tự vì code và ADR trỏ tới, ví dụ `TODO(prod #4)`):

| # | File | Vấn đề | Fix | Trạng thái |
|---|------|--------|-----|------------|
| 1 | `UserManagement.API/Extensions/OpenIddictServerExtensions.cs` | Ký/mã hoá token bằng cert self-signed tự tạo (`GetOrCreateDevCert`), lưu ở `OpenIddict:KeyDir` (prod: volume `openiddict_keys`) — sống qua restart nhưng không phải cert thật | Dùng X.509 cert thật (Key Vault…) cho `AddSigningCertificate()` / `AddEncryptionCertificate()` | Còn mở |
| 2 | Same file | `DisableTransportSecurityRequirement()` cho phép HTTP | Chỉ gọi khi không phải Production | ✅ Đã xử lý |
| 3 | `Program.cs` của UserManagement.API, FishDex.API, AquaHome.API, AquaHome.Worker | `AutoMigrate:OnStartup` — auto-migrate khi startup, race condition nếu multi-instance | Tắt flag ở prod, chạy `dotnet ef database update` như một bước riêng trước deploy | Còn mở |
| 4 | `Share/FishLover.Shared/Extensions/ResourceServerExtensions.cs` | `ValidateAudience = false` trong scheme "OpenIddict" (FishDex + AquaHome) — ADR 0004 | Đăng ký FishDex/AquaHome như resource server trong OpenIddict, bật validate audience ở đúng chỗ này | Còn mở |
| 5 | `UserManagement.API/appsettings.Docker.json` | `OpenIddict:Issuer = http://localhost:8080` | Prod chạy môi trường `Production` và đặt `OpenIddict__Issuer=${OIDC_ISSUER}` (VM1 compose); file Docker chỉ dùng cho local | ✅ Đã xử lý |
| 6 | Docker Compose | DataProtection keys không được persist ngoài container | Mount volume hoặc dùng Key Vault / KMS | Còn mở |
