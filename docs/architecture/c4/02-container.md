[← Mục lục C4](./README.md)

# FishLover — C4 Level 2: Container

> Cập nhật: 2026-11-26 (Worker đăng video dự thi — ADR 0005; app mobile và repo AI — ADR 0006) · Bóc tách box "FishLover Platform" ở [Level 1](./01-system-context.md).
>
> Boundary ở đây là **hệ thống phần mềm**, không phải máy chủ. Container nào chạy trên VM nào xem [Deployment](./deployment.md).

## Diagram

```mermaid
%%{init: {"flowchart": {"curve": "basis", "nodeSpacing": 50, "rankSpacing": 90}}}%%
graph LR
    USER(["👤 Người chơi cá cảnh<br/>[Person]"])
    ADMIN(["🛡️ Admin / Content Manager<br/>[Person]"])

    subgraph SYS ["🐟 FishLover Platform"]
        direction TB
        SPA["Single-Page App<br/>[React 19 · Vite 6 · TypeScript]<br/>Giao diện web, mobile-first"]
        MOB["Mobile App 🚧<br/>[React Native · Expo]<br/>iOS + Android, dùng chung @fishlover/shared"]
        GW["API Gateway<br/>[.NET 9 · Ocelot 24]<br/>Routing + rate limit"]
        CACHE[("Cache / session<br/>[Redis 7]")]

        subgraph G_UM ["UserManagement"]
            direction TB
            UM["API<br/>[.NET 9 · ASP.NET Core · OpenIddict]<br/>Đăng nhập, JWT, web push"]
            UM_DB[("db: usermanagement<br/>[PostgreSQL 16]")]
        end

        subgraph G_FD ["FishDex"]
            direction TB
            FD["API<br/>[.NET 9 · ASP.NET Core]<br/>Loài cá, Market, tra cứu"]
            FD_DB[("db: fishdex<br/>[PostgreSQL 16 + pgvector]")]
        end

        subgraph G_AQ ["AquaHome"]
            direction TB
            AQ["API (BFF)<br/>[.NET 9 · ASP.NET Core]<br/>Hồ cá, snapshot, contest, bài viết"]
            WK["Worker<br/>[.NET 9 Worker Service · 1 instance]<br/>Đăng video dự thi, dọn staging,<br/>nhắc lịch, sync market, sync view count"]
            AQ_DB[("db: aquahome<br/>[PostgreSQL 16]")]
        end

        subgraph G_AI ["AI Services 🚧 chưa triển khai · repo FishLover-AI"]
            direction TB
            EMB["Embedding Service<br/>[Python · FastAPI · MiniLM-L6-v2]"]
            IMG["Image Search Service<br/>[Python · FastAPI · CLIP ViT-B/32]"]
            OLLAMA["Ollama<br/>[Gemma 2B — LLM dự phòng]"]
        end
    end

    R2(["🗄️ Cloudflare R2<br/>[Object storage · S3 API]"])
    YT(["▶️ YouTube<br/>[Data API v3]"])
    RESEND(["✉️ Resend<br/>[Email API]"])
    GROQ(["☁️ Groq API<br/>[LLM inference]"])
    PUSH(["🔔 Push mobile 🚧<br/>[Expo Push / FCM · APNs]"])
    FB(["📦 FishBase Dataset<br/>[Parquet]"])

    USER -->|"tra cứu, quản lý hồ, dự thi<br/>[HTTPS]"| SPA
    ADMIN -->|"duyệt bài viết, quản lý contest<br/>[HTTPS]"| SPA
    SPA -->|"REST + JWT Bearer<br/>[HTTPS/JSON]"| GW
    SPA -->|"upload ảnh/video: presigned PUT<br/>tải ảnh: presigned GET<br/>[HTTPS]"| R2
    USER -.->|"cùng tính năng trên điện thoại"| MOB
    MOB -.->|"REST + JWT Bearer, cùng /v1<br/>[HTTPS/JSON]"| GW
    MOB -.->|"presigned PUT/GET<br/>[HTTPS]"| R2

    GW -->|"/user/v1 · /connect<br/>[HTTP/JSON]"| UM
    GW -->|"/fishdex/v1<br/>[HTTP/JSON]"| FD
    GW -->|"/aquahome/v1<br/>[HTTP/JSON]"| AQ

    AQ -->|"gửi web push<br/>[HTTP + internal key]"| UM
    AQ -->|"species summary cho snapshot<br/>[HTTP + internal key]"| FD
    WK -->|"POST /api/push/send<br/>[HTTP + internal key]"| UM
    WK -->|"POST /api/market/ingest · 8h<br/>[HTTP + internal key]"| FD

    UM -->|"[Npgsql · 5432]"| UM_DB
    FD -->|"[Npgsql · 5432]"| FD_DB
    AQ -->|"[Npgsql · 5432]"| AQ_DB
    WK -->|"[Npgsql · 5432]"| AQ_DB
    UM -->|"session<br/>[RESP · 6379]"| CACHE
    FD -->|"hạn mức khách<br/>[RESP]"| CACHE
    AQ -->|"cache<br/>[RESP]"| CACHE

    UM -->|"email xác thực, reset mật khẩu<br/>[HTTPS REST]"| RESEND
    FD -->|"ký presigned URL ảnh loài<br/>[S3 API]"| R2
    AQ -->|"ký presigned URL · kiểm file đã tải lên<br/>[S3 API]"| R2
    AQ -->|"công khai / xoá video khi duyệt<br/>[Data API v3]"| YT
    WK -->|"tải video staging, xoá staging<br/>[S3 API]"| R2
    WK -->|"đăng video trong hàng chờ · 2 phút<br/>đồng bộ lượt xem · 6h<br/>[Data API v3]"| YT
    FB -->|"nạp dữ liệu loài — offline, 1 lần<br/>[Parquet import]"| FD

    AQ -.->|"embed câu hỏi cho RAG<br/>[HTTP/JSON]"| EMB
    FD -.->|"embed ảnh để tìm loài<br/>[HTTP/JSON]"| IMG
    EMB -.->|"sinh câu trả lời<br/>[HTTPS REST]"| GROQ
    EMB -.->|"dự phòng khi Groq lỗi >5 phút<br/>[HTTP]"| OLLAMA
    UM -.->|"thông báo đẩy cho app<br/>[HTTPS]"| PUSH

    classDef planned stroke-dasharray: 5 4;
    class MOB,EMB,IMG,OLLAMA,PUSH planned;
```

## Legend

| Ký hiệu | Nghĩa |
|---|---|
| Nét liền | Quan hệ **đang chạy thật** trên production |
| Nét đứt + 🚧 | **Chưa triển khai** — kiến trúc đích, chưa có code chạy |
| `[...]` trong nhãn | Technology / giao thức |
| Hình trụ | Data store |
| Hình bo tròn ngoài khung | Person hoặc external system (ngoài tầm kiểm soát của dự án) |

Chỉ có **hai** kiểu đường: liền = live, đứt = planned. Quan hệ nạp dữ liệu FishBase tuy chạy offline một lần vẫn vẽ nét liền vì nó đã thực sự chạy — tính chất "một lần" ghi trong nhãn chứ không mã hoá bằng kiểu nét.

## Container catalog

| Container | Technology | Trách nhiệm |
|---|---|---|
| Single-Page App | React 19 · Vite 6 · TypeScript 5.6 | Toàn bộ giao diện (mobile-first, iPhone 12+). Upload/tải media trực tiếp với R2 qua presigned URL |
| Mobile App 🚧 | React Native · Expo (EAS Build/Submit/Update) | App iOS + Android, nằm ở `FrontEnd/apps/mobile`, dùng chung `@fishlover/shared` với SPA (ADR 0006). Gọi cùng API `/v1` qua gateway |
| API Gateway | .NET 9 · Ocelot 24 | Điểm vào duy nhất của API. Route `/user/v1`, `/fishdex/v1`, `/aquahome/v1` + rate limit 20–60 req/phút tuỳ route |
| UserManagement API | .NET 9 · ASP.NET Core · OpenIddict · Identity | OAuth2/OIDC, JWT, đăng ký theo lời mời, web push (VAPID) |
| FishDex API | .NET 9 · ASP.NET Core | Dữ liệu loài cá, Market theo quốc gia, tên gọi cộng đồng, hạn mức tra cứu cho khách chưa đăng nhập |
| AquaHome API (BFF) | .NET 9 · ASP.NET Core | Hồ cá, snapshot, contest, bài viết. Điều phối gọi sang FishDex/UserManagement. Xác nhận bài dự thi chỉ kiểm file trên R2 rồi đưa vào hàng chờ; công khai/xoá video YouTube khi admin duyệt/từ chối |
| AquaHome Worker | .NET 9 Worker Service | Job nền: **đăng video dự thi trong hàng chờ lên YouTube (2 phút, giữ lượt đăng theo ngày giờ Pacific — ADR 0005)**, dọn video staging quá hạn (6h), nhắc lịch chăm hồ, đẩy market species sang FishDex (8h), đồng bộ lượt xem YouTube (6h). **Chỉ chạy một instance** |
| db: usermanagement | PostgreSQL 16 | Tài khoản, token, subscription push |
| db: fishdex | PostgreSQL 16 + pgvector | Loài cá, media, market listing. Extension pgvector bật bằng script khởi tạo DB. Code chưa có entity vector, nhưng DB có thể còn bảng `SpeciesChunks` rỗng từ migration cũ đã bị gỡ khỏi code (xem kế hoạch AI, mục Bối cảnh) |
| db: aquahome | PostgreSQL 16 | Hồ cá, snapshot, contest, bài viết |
| Cache / session | Redis 7 | Session (UserManagement), cache (AquaHome), hạn mức khách vãng lai (FishDex) |
| Embedding Service 🚧 | Python · FastAPI · all-MiniLM-L6-v2 · code ở repo `FishLover-AI` (ADR 0006) | Sinh embedding câu hỏi cho RAG, gọi LLM sinh câu trả lời |
| Image Search Service 🚧 | Python · FastAPI · CLIP ViT-B/32 · repo `FishLover-AI` | Sinh embedding ảnh người dùng upload để tìm loài tương tự |
| Ollama 🚧 | Gemma 2B | LLM chạy nội bộ, dự phòng khi Groq lỗi |

## External systems

| System | Container gọi nó | Giao thức | Mục đích |
|---|---|---|---|
| Cloudflare R2 | SPA, FishDex API, AquaHome API, AquaHome Worker | HTTPS / S3 API | Backend **ký** presigned URL; **SPA mới là bên thực sự** PUT/GET dữ liệu. AquaHome API kiểm file (HEAD) lúc xác nhận; Worker tải video staging để đẩy lên YouTube rồi xoá |
| YouTube | AquaHome Worker, AquaHome API | YouTube Data API v3 | Worker đăng video dự thi (unlisted) và đồng bộ lượt xem; API công khai hoặc xoá video khi admin duyệt/từ chối |
| Resend | UserManagement API | HTTPS REST | Email xác thực tài khoản, reset mật khẩu |
| Groq API | Embedding Service 🚧 | HTTPS REST | Sinh câu trả lời. Giới hạn 30 RPM, lỗi 429 → trả 503 về FE, không tự retry |
| FishBase Dataset | FishDex API | Parquet import | Nạp dữ liệu loài gốc — offline, chạy một lần |
| Push mobile 🚧 | UserManagement API | HTTPS | Thông báo đẩy cho app (Expo Push hoặc FCM/APNs, chưa chốt). Web vẫn dùng Web Push (VAPID) |

## Quyết định thiết kế đáng chú ý

- **Ba database vẽ tách rời nhau** dù hiện chạy trên cùng một PostgreSQL container: mỗi service chỉ chạm database của mình, migration riêng, không có foreign key xuyên service — tách vật lý được mà không đổi code. Số container đang chạy thật xem ở [Deployment](./deployment.md).
- **Redis vẽ chung một node** vì đây đúng là một keyspace dùng chung, không phải ba store độc lập.
- **Worker của AquaHome ghi dữ liệu market sang FishDex qua HTTP API** (`POST /api/market/ingest`, idempotent theo khoá), **không** ghi thẳng vào database của FishDex — boundary dữ liệu vẫn do FishDex giữ. Endpoint này đòi internal API key; thiếu key thì tự trả 503 chứ không mở toang.
- **Redis chết thì hạn mức tra cứu của FishDex fail-open** (cho xem tiếp) — ưu tiên không chặn người dùng hơn là siết chính xác hạn mức.
- **Video dự thi được đăng lên YouTube ở Worker, không trong request xác nhận** (ADR 0005): request chỉ HEAD file trên R2 rồi đưa bài vào hàng chờ, nên không còn kẹt vì timeout của FE/nginx. Hết lượt đăng trong ngày thì bài chờ sang ngày sau.
- **App mobile ở cùng repo với SPA, dịch vụ ML ở repo riêng** (ADR 0006): chia theo công nghệ và đích chạy. Phần tích hợp AI trong .NET (`ILlmProvider`, RAG, entity vector) vẫn ở repo này.
- **Upload/tải media không đi qua backend**: backend chỉ ký URL, dữ liệu đi thẳng giữa trình duyệt và R2 — tiết kiệm băng thông VM, nhưng nghĩa là R2 phải cấu hình CORS đúng cho domain FE.

## Chưa chốt

- **Ai ghi embedding vào database FishDex?** Thiết kế v2.0 nói `SpeciesChunk.Embedding` (384-d) và `SpeciesMedia.ClipEmbedding` (512-d) nằm trong db `fishdex`, nhưng **trong code chưa có entity nào như vậy** và cũng chưa có service nào sinh/ghi chúng. Diagram vì thế không vẽ mũi tên ghi vector — cần chốt trước khi làm Story 2.1 / 4.1.
- Image Search Service hiện chỉ có chiều FishDex gọi sang; chiều đọc vector từ db `fishdex` phụ thuộc câu hỏi trên.
- **Kênh push cho app mobile**: Expo Push (đơn giản, qua dịch vụ của Expo) hay gọi thẳng FCM/APNs. Cả hai đặt sau `IWebPushNotifier`.
- **Trước khi app mobile dùng đăng nhập**: validate audience (ADR 0004) và discovery OIDC — ở local, `/.well-known/openid-configuration` trả `authorization_endpoint` là host nội bộ `usermanagement-api:8080`; web không đọc discovery nên chưa lộ ra, AppAuth của mobile thì có. Chưa kiểm trên PROD.

## Ghi chú phạm vi — không vẽ ở Level 2

- **Hạ tầng triển khai** (VM1/VM2/VM3, Nginx reverse proxy, Cloudflare Pages, Docker network, IP private) → [Deployment](./deployment.md).
- **Monitoring/observability** (Prometheus, Loki, Tempo, Grafana, Promtail, node-exporter, cAdvisor) → cũng thuộc deployment view; chúng không phục vụ người dùng cuối nên không phải container của hệ thống phần mềm.
- **CI/CD, backup, gia hạn chứng chỉ, quản lý secret** — cố ý không vẽ: thuộc quy trình vận hành, không phải cấu trúc phần mềm.

## Known drift (repo lệch thực tế)

| Chỗ lệch | Chi tiết |
|---|---|
| Route AI chưa có | Thiết kế nói AquaHome/FishDex gọi AI service "qua gateway" (`/embeddings/*`, `/image-search/*`) nhưng ocelot chưa khai báo route nào như vậy |
