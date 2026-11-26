# FishLover — Hướng dẫn cho BE Developer

> Viết lại 26/11/2026 từ code trên `release/v1.2` (bản cũ 07/2026 đã lệch nhiều và được gỡ). Mọi tên class, thư mục, route dưới đây đã đối chiếu với code.
> Lệnh build/run/test/migration và các quy tắc bắt buộc nằm ở [`BackEndProject/CLAUDE.md`](../../BackEndProject/CLAUDE.md) — guide này không chép lại, chỉ giải thích code được tổ chức thế nào và đi từ đâu.

## Đọc kèm

| Tài liệu | Dùng để |
|---|---|
| [C4 Container](../architecture/c4/02-container.md), [Deployment](../architecture/c4/deployment.md) | Bức tranh hệ thống, container nào chạy ở VM nào |
| [`BackEndProject/GLOSSARY.md`](../../BackEndProject/GLOSSARY.md) | Thuật ngữ: FishBase species, Community species, Species Profile, Thumbnail, Upload verification… |
| [`BackEndProject/docs/adr/`](../../BackEndProject/docs/adr/) | Quyết định đã chốt (đọc trước khi đề xuất ngược lại) |
| [`BackEndProject/CLAUDE.md`](../../BackEndProject/CLAUDE.md) | Lệnh, cổng, route gateway, luồng auth, checklist production |

## 1. Bức tranh chung

```
Trình duyệt (aquahome-web) ──HTTPS──► nginx ──► ApiGateway (Ocelot, :5000)
                                                  ├─ /.well-known/*, /connect/*, /user/v1/*  ► UserManagement (:8080)
                                                  ├─ /fishdex/v1/*                           ► FishDex (:8081)
                                                  └─ /aquahome/v1/*                          ► AquaHome (:8082)
AquaHome ──HTTP nội bộ──► FishDex (FishDexClient), UserManagement (/api/push/send)
AquaHome.Worker ──► AquaHome DB, FishDex (/api/market/ingest), YouTube
```

- Ba service, mỗi service một database PostgreSQL riêng (FishDex có thêm pgvector). Redis dùng cho cache/hạn mức.
- Gateway đổi tiền tố: `/fishdex/v1/x` → `fishdex:8081/api/x`. Controller luôn có route bắt đầu bằng `api/`.
- Ảnh/video nằm trên Cloudflare R2; FE tải lên thẳng R2 bằng presigned PUT rồi gọi API "confirm" (xem mục 5).

## 2. Một service được chia thế nào

Mỗi service theo cùng một khuôn:

| Project | Chứa | Ghi chú |
|---|---|---|
| `*.API` | `Program.cs`, controllers, filters, middleware, hosted services | Controller mỏng: nhận request → gọi service ở Domain → trả DTO |
| `*.Domain` | Services (nghiệp vụ), DTOs, mapping `ToDto()`, settings | Đăng ký DI qua `Extensions/ServiceCollectionExtensions.cs` (vd. `AddUserManagementDomain`) |
| `*.EFCore` | `DbContext`, entities, migrations, repositories | Repository cho truy vấn; AquaHome giữ Generic Repository cho Aquarium (ADR 0003) |
| `AquaHome.Common` | Enum dùng chung giữa API, Domain, Worker | |
| `AquaHome.Worker` | Job nền (Generic Host, không có Kestrel) | |

`Program.cs` của các API giống nhau ở xương sống: Serilog → `AddFishLoverTelemetry` → auth (xem mục 4) → Swagger (Production tắt, trừ FishDex) → `MigrateDatabaseIfEnabledAsync<TContext>()` → `MapHealthChecks("/health")` + `MapPrometheusScrapingEndpoint()`.

## 3. Từng service

### UserManagement — tài khoản và đăng nhập
- **OAuth2/OIDC server** bằng OpenIddict: `Controllers/Connect/AuthorizationController.cs` (`/connect/authorize`, `/connect/token`, `/connect/userinfo`, `/connect/logout`, trang đăng nhập `/connect/login`). FE đăng nhập bằng Authorization Code + PKCE. Client được seed bởi `Services/OpenIddictSeeder.cs` (`aquahome-fe`, `FishDex_Swagger`); cấu hình server ở `Extensions/OpenIddictServerExtensions.cs`.
- **Đăng ký bằng mã OTP** (`AuthController`, `/api/auth/*`): `valid-email/{email}` gửi mã 8 ký tự (chỉ nhận @gmail.com, 1 mã/phút, hết hạn 5 phút) → `register` (mã dùng 1 lần, quá 5 lần sai thì huỷ mã). `RequireInvitation` bật thì bắt buộc mã mời (`Invitation`, `InvitationUsed`). Quên mật khẩu: `forgot-password` → `reset-password`. Toàn bộ nằm trong `UserManagement.Domain/Services/AuthService.cs`.
- `login`/`refresh`/`logout` của `AuthController` là luồng JWT cũ (`JwtSettings`); FE không dùng.
- **Quản lý user** (admin): `UsersController`; hồ sơ của chính mình: `AccountController`.
- **Web Push**: `PushController` (`/api/push`) lưu subscription; `/api/push/send` chỉ nhận internal API key (AquaHome gọi để nhắc lịch).
- Email qua Resend (`EmailProviders/ResendEmailProvider.cs`), template `Helper/EmailTemplates/{en,vi}/` (VerificationCode, PasswordReset, WelcomeEmail).

### FishDex — tra cứu loài
- **Dữ liệu FishBase** nạp bằng ETL Python (`Pipeline/local/FishDex/etl/`), entity chia theo nhóm trong `FishDex.EFCore/Entity/` (`Species`, `Ecologies`, `Ecosystem`, `MorphData`, `Occurrence`, `Stocks`, `Media`, `Market`, `Cache`).
- **Species Profile** (`FishDex.Domain/SpeciesProfiles/`): mọi màn hình hiển thị loài đọc qua module này nên cùng loài luôn cùng ảnh, cùng tên. Đọc thẳng bảng FishBase, không cache tầng DB (ADR 0001).
- **Community species** (cá lai… do người dùng gửi, mã từ 500.000): `CommunitySpeciesService`, `CommunityCommonNameService`; lưu chính ở `SpeciesSnapshots`. Tên bản ngữ không có FK tới `Species` (ADR 0002). Ảnh mồ côi/quá cỡ được dọn định kỳ bởi `CommunityImageJanitor` qua `BackgroundServices/CommunityImageCleanupService.cs`.
- **Market layer** (loài bán ở từng nước): `MarketController`, `MarketAdminController`, `MarketIngestController` (`/api/market/ingest`, chỉ internal API key — AquaHome.Worker đẩy dữ liệu vào).
- **Truy cập công khai**: `PublicSpeciesController` (`/api/public/species/*`) có hạn mức xem cho khách chưa đăng nhập (`AnonSpeciesQuotaFilter` + `AnonQuotaService`, đếm trên Redis, Redis chết thì cho qua); `PublicMediaAccessController` phát link media có chữ ký (`MediaAccessSigner`, khoá `MediaAccess:Key`).

### AquaHome — bể nuôi và cộng đồng
- **Bể và cá**: `AquariumsController`, `AquariumService` (đếm cá đúng nhờ `GetByIdAndUserWithFishAsync`), ảnh bể `AquariumMediaController`/`AquariumMediaService`.
- **Đăng bể công khai**: `AquariumSnapshotController`, `PublicSnapshotsController`, `SnapshotService` — snapshot lưu JSONB, đọc/ghi qua một đường duy nhất (`SnapshotDataReader`). Dữ liệu loài lấy từ FishDex qua `FishDexClient`; FishDex lỗi/không có mặt thì ném `FishDexUnavailableException` → 503, không trả rỗng âm thầm.
- **Cuộc thi**: `ContestsController`, `ContestService`, `YouTubeUploadService` — video tải lên R2 (staging) → confirm → admin duyệt thì đưa lên YouTube.
- **Bài viết**: `ArticlesController` (đọc), `AdminArticlesController` (soạn), `ArticleService`; nội dung đa ngôn ngữ (`ArticleTranslation`). Mọi đường trả bài cho người đọc đi qua `AquaHome.Domain/Articles/PublicArticleView.cs`: chọn bản dịch (đúng ngôn ngữ → en → vi → bất kỳ) và khoá bài (khách chỉ đọc bài Beginner; bài khoá vẫn có metadata nhưng không bao giờ nhận URL nội dung hay ảnh). Trang mới hiển thị bài phải gọi module này, đừng tự dựng DTO.
- **Slug** của bài viết, bể đã đăng và tag dùng chung `AquaHome.Domain/Slugs/Slug.cs` (bỏ dấu kể cả `đ`, tối đa 150 ký tự, thêm hậu tố `-2`, `-3` khi trùng; cột `Slug` có unique index).
- **Khác**: yêu thích (`FavoritesController`), đã xem gần đây (`RecentlyViewedController`), nhắc việc (`RemindersController`, `UserRemindersController`), hạn mức theo role (`QuotaService`, `AdminQuotaController`; `ConsumeDailyAsync` để dành cho quota AI theo ngày).
- **Worker** (`AquaHome.Worker/Services/`): `TaskReminderBackgroundService` (gửi push nhắc lịch qua UserManagement), `SyncContestViewCountsBackgroundService`, `SyncMarketSpeciesBackgroundService`, `CleanupContestStagingBackgroundService`.
- Lỗi nghiệp vụ được đổi thành mã HTTP bằng filter trong `AquaHome.API/Filters/` (quota → 429, contest không hợp lệ → 422, R2 quá tải → 503, FishDex down → 503, bài viết sai cấu trúc → 422).

### Share — code dùng chung
- `FishLover.Shared`: `AddFishLoverJwtAuthentication`/`AddFishLoverAuthorization` (ApiGateway, UserManagement), `AddFishLoverResourceServer` (FishDex, AquaHome — scheme "OpenIddict"), `AddFishLoverInternalApiKey`, `AddFishLoverTelemetry`/`AddFishLoverWorkerTracing`, `MigrateDatabaseIfEnabledAsync`, `CurrentUserSession`, `PagedResult<T>`.
- `FishLover.Storage`: `IStorageService` (adapter `S3StorageService` cho R2, `InMemoryStorageService` cho test), `UploadVerifier`, `UploadContentTypes`, `MediaAccessSigner`.

## 4. Xác thực giữa các phần

| Ai gọi | Bằng gì | Service kiểm bằng |
|---|---|---|
| FE → FishDex/AquaHome | Access token OpenIddict (PKCE) | `AddFishLoverResourceServer` — chưa validate audience (ADR 0004, checklist prod #4) |
| FE → UserManagement | Access token OpenIddict hoặc JWT cũ | Default policy chấp nhận cả hai scheme |
| AquaHome → UserManagement/FishDex | Header internal API key | `AddFishLoverInternalApiKey`, policy `InternalApiKey.PolicyName` |

Trong code nghiệp vụ, lấy người dùng hiện tại qua `ICurrentUserSession` (đừng đọc claim trực tiếp từ `HttpContext`).

## 5. Quy ước hay vướng

Danh sách đầy đủ ở mục *Rules* của `BackEndProject/CLAUDE.md`; ba điều hay sai nhất:

- **Object key**: không tự ghép đường dẫn storage. Ảnh FishBase dùng `SystemImage.ObjectKey`; key khác của FishDex qua `FishDexObjectKeys`, của AquaHome qua `AquaHomeObjectKeys`. Key đã lưu trong DB — đổi format là mất file cũ (có test khoá từng format).
- **Upload qua presigned PUT** không tự chặn dung lượng: bước confirm phải kiểm file thật qua `UploadVerifier` (HEAD, so dung lượng, xoá file quá cỡ) — xem *Upload verification* trong GLOSSARY.
- **Route gateway**: thêm route thì thêm vào cả `ocelot.json`, `ocelot.Development.json`, `ocelot.Docker.json` (mỗi môi trường nạp đúng một file).

## 6. Test

| Project | Kiểu | Cần Docker |
|---|---|---|
| `Share/FishLover.Storage.Tests` | Unit: `UploadVerifier`, `UploadContentTypes`, `MediaAccessSigner`, `InMemoryStorageService` | Không |
| `Share/FishLover.Shared.Tests` | Unit: internal API key auth | Không |
| `AquaHome/AquaHome.Tests` | Service với repo/storage giả (contest confirm upload, aquarium media…) + khoá format `AquaHomeObjectKeys` | Không |
| `FishDex/FishDex.Tests` | Qua interface module (Species Profile, community) trên Postgres thật (Testcontainers) + khoá format `FishDexObjectKeys` | Có |
| `UserManagement/UserManagement.Tests` | HTTP end-to-end: dựng cả API bằng `WebApplicationFactory` trên Postgres thật, email giả (`FakeEmailService`) | Có |

Cách viết test của dự án: **test qua interface của module** (service hoặc HTTP), không test chi tiết bên trong; phụ thuộc ra ngoài (storage, email, YouTube) thay bằng adapter giả; DB dùng Postgres thật thay vì provider in-memory để không lệch hành vi SQL.

## 7. Thêm một tính năng — trình tự gợi ý

1. Đặt tên khái niệm mới cho khớp `GLOSSARY.md` (thêm thuật ngữ nếu cần).
2. Entity + migration trong `*.EFCore` (lệnh ở CLAUDE.md; FishDex bắt buộc có `--startup-project`).
3. Service trong `*.Domain`, đăng ký ở `ServiceCollectionExtensions`; DTO + `ToDto()`.
4. Controller mỏng trong `*.API`; lỗi nghiệp vụ → exception riêng + filter (AquaHome) thay vì `try/catch` trả 500.
5. Route mới phải ra ngoài internet? Thêm vào cả 3 file ocelot.
6. Test qua interface (mục 6); `dotnet build` phải 0 cảnh báo.
7. Quyết định khó đảo ngược, có đánh đổi thật → ghi ADR trong `BackEndProject/docs/adr/`.
8. Commit BE và FE tách riêng (trừ khi sửa bug).
