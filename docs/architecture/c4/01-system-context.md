[← Mục lục C4](./README.md)

# FishLover — C4 Level 1: System Context

> Cập nhật: 2026-11-26 · BackEndProject: ApiGateway + AquaHome + FishDex + UserManagement; FE: aquahome-web; app mobile 🚧 và dịch vụ AI 🚧 thuộc phase 2.0 (ADR 0006)

## Diagram

```mermaid
graph TB
    User(["👤 Người chơi cá cảnh<br/>(Web · App iOS/Android 🚧)"])
    Admin(["🛡️ Admin / Content Manager<br/>(cùng web app, quyền cao hơn)"])

    subgraph FishLover ["🐟 FishLover Platform"]
        PLATFORM["AquaHome · FishDex · UserManagement<br/>Quản lý hồ cá · Tra cứu loài cá (FishDex)<br/>Bài viết · Market · Contest · AI Q&A"]
    end

    EXT_GROQ(["☁️ Groq API<br/>(LLM inference)"])
    EXT_R2(["🗄️ Cloudflare R2<br/>(Object storage - ảnh, media)"])
    EXT_YT(["▶️ YouTube<br/>(Video hosting - contest entries)"])
    EXT_RESEND(["✉️ Resend<br/>(Email - xác thực, reset mật khẩu)"])
    EXT_FB(["📦 FishBase Dataset<br/>(Seed data loài cá, offline)"])
    EXT_PUSH(["🔔 Push mobile 🚧<br/>(Expo Push / FCM · APNs)"])

    User -->|"HTTPS"| PLATFORM
    Admin -->|"HTTPS"| PLATFORM
    PLATFORM -->|"LLM Q&A / RAG"| EXT_GROQ
    PLATFORM -->|"Upload / lấy ảnh, media"| EXT_R2
    PLATFORM -->|"Upload video contest"| EXT_YT
    PLATFORM -->|"Gửi email"| EXT_RESEND
    EXT_FB -->|"Seed dữ liệu loài cá (1 lần)"| PLATFORM
    PLATFORM -.->|"Thông báo đẩy cho app 🚧"| EXT_PUSH

    style PLATFORM fill:#1a3320,stroke:#3fb950,color:#e6edf3
    style User fill:#1f3a5f,stroke:#58a6ff,color:#e6edf3
    style Admin fill:#1f3a5f,stroke:#58a6ff,color:#e6edf3
    style EXT_GROQ fill:#161b22,stroke:#f0883e,color:#e6edf3
    style EXT_R2 fill:#161b22,stroke:#e3b341,color:#e6edf3
    style EXT_YT fill:#161b22,stroke:#f85149,color:#e6edf3
    style EXT_RESEND fill:#161b22,stroke:#79c0ff,color:#e6edf3
    style EXT_FB fill:#161b22,stroke:#6e7681,color:#e6edf3
    style EXT_PUSH fill:#161b22,stroke:#a371f7,color:#e6edf3,stroke-dasharray: 5 4
```

## Actors

| Actor | Mô tả |
|---|---|
| Người chơi cá cảnh | End user chính, dùng qua web app (mobile-first, iPhone 12+) và sắp tới app iOS/Android 🚧 — quản lý hồ cá, tra loài cá, đọc bài viết, tham gia contest, hỏi đáp AI |
| Admin / Content Manager | Cùng web app, quyền cao hơn — duyệt bài viết (`AdminArticlesController`), quản lý contest/sponsor |

## External Systems

| System | Vai trò | Dùng bởi service nào (nội bộ) |
|---|---|---|
| Groq API | LLM inference cho tính năng AI Q&A/RAG. Rate limit 30 RPM, fallback nội bộ sang Ollama Gemma 2B (self-hosted trên VM3) khi Groq down >5 phút | AquaHome BFF → VM3 embedding service |
| Cloudflare R2 | Object storage (S3-compatible) — ảnh species, ảnh hồ cá, snapshot, media contest | AquaHome, FishDex (`S3StorageService`) |
| YouTube | Lưu trữ & phát video contest entries | AquaHome Worker đăng video (`ContestUploadQueue`), AquaHome API công khai/xoá khi duyệt (`YouTubeUploadService`) |
| Resend | Email provider — xác thực tài khoản, reset mật khẩu | UserManagement (`ResendEmailProvider`) |
| Push mobile 🚧 | Gửi thông báo đẩy tới app iOS/Android (Expo Push hoặc FCM/APNs, chưa chốt) | UserManagement (sau `IWebPushNotifier`) |
| FishBase Dataset | Nguồn dữ liệu gốc (Parquet) để seed database loài cá — chỉ chạy 1 lần / khi cập nhật data, không phải runtime dependency | FishDex (`FishBaseFlattener`) |

## Ghi chú phạm vi (không vẽ ở Level 1)

- **Hạ tầng deploy** (Cloudflare Pages cho FE, Oracle VM1/VM2/VM3, Docker) — thuộc Deployment/Container view, không phải Context.
- **Monitoring stack** (Prometheus/Loki/Grafana trên VM2) — là internal ops tooling, không phải actor hay external system mà user tương tác.
- **Ollama (self-hosted)** — là fallback nội bộ của AI stack (chạy trên VM3), không phải third-party bên ngoài nên gộp vào box "FishLover Platform", không tách riêng ở Context level.
- **Notion** — công cụ quản lý task nội bộ team, không phải runtime dependency của hệ thống đang chạy.
- **App Store / Google Play / Expo EAS** — kênh build và phát hành app mobile, không phải runtime dependency (ghi ở [Deployment](./deployment.md)).

Chi tiết bên trong "FishLover Platform" (SPA, API Gateway, 3 service BE, database, cache, AI services) nằm ở [Level 2 — Container](./02-container.md). Container nào chạy trên máy chủ nào xem [Deployment](./deployment.md).
