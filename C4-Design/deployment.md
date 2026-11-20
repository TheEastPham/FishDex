[← Về danh sách (00)](./00-overview.html)

# FishLover — Deployment Diagram

> Cập nhật: 2026-10-01 · Các container ở [Level 2](./02-container.md) thực sự chạy ở đâu.
>
> **Deployment nằm trong C4, nhưng không phải một "level".** C4 có 4 level cốt lõi (Context → Container → Component → Code) cộng với các **diagram bổ sung**: System Landscape, Dynamic và Deployment. Vì vậy file này cố ý **không đánh số** — dãy `01/02/03/04` chỉ dành cho 4 level. Level 3 (Component) là bóc tách bên trong *một* container, hiện chưa cần.

## Diagram

```mermaid
%%{init: {"flowchart": {"curve": "basis", "nodeSpacing": 50, "rankSpacing": 90}}}%%
graph LR
    BROWSER(["👤 Trình duyệt người dùng"])

    subgraph CF ["☁️ Cloudflare"]
        direction TB
        PAGES["Cloudflare Pages<br/>[Static hosting]<br/>SPA · fishlover.org"]
        R2["Cloudflare R2<br/>[Object storage]"]
    end

    subgraph VM1 ["🖥️ VM1 — Oracle ARM Ubuntu 24.04 · 10.0.0.64 · 12GB RAM"]
        direction TB
        NGINX["nginx<br/>[Docker] :80/:443<br/>TLS termination"]
        GW_C["gateway<br/>[Docker · .NET 9] :5000"]
        UM_C["usermanagement<br/>[Docker · .NET 9] :8080"]
        FD_C["fishdex<br/>[Docker · .NET 9] :8081"]
        AQ_C["aquahome<br/>[Docker · .NET 9] :8082"]
        WK_C["aquahome-worker<br/>[Docker · .NET 9]"]
        PG_C[("postgres<br/>[Docker · pgvector/pg16]<br/>3 database: usermanagement · fishdex · aquahome")]
        RD_C[("redis<br/>[Docker · redis:7-alpine]")]
        AGENTS["promtail · node-exporter · cadvisor<br/>[Docker]<br/>bind private IP, không hở public"]
    end

    subgraph VM2 ["📊 VM2 — Monitoring · 10.0.0.94"]
        direction TB
        PROM["prometheus"]
        LOKI["loki"]
        TEMPO["tempo<br/>OTLP :4317"]
        GRAF["grafana<br/>dashboard + alert"]
    end

    subgraph VM3 ["🤖 VM3 — AI Services 🚧 chưa deploy · 6GB RAM"]
        direction TB
        EMB_C["embedding_service<br/>[Python · FastAPI] :8000"]
        IMG_C["image_search_service<br/>[Python · FastAPI] :8001"]
    end

    BROWSER -->|"HTTPS"| PAGES
    BROWSER -->|"HTTPS · api.fishlover.org"| NGINX
    BROWSER -->|"presigned PUT/GET [HTTPS]"| R2

    NGINX --> GW_C
    GW_C --> UM_C
    GW_C --> FD_C
    GW_C --> AQ_C
    UM_C --> PG_C
    FD_C --> PG_C
    AQ_C --> PG_C
    WK_C --> PG_C
    UM_C --> RD_C
    FD_C --> RD_C
    AQ_C --> RD_C
    FD_C -->|"S3 API"| R2
    AQ_C -->|"S3 API"| R2

    GW_C -.->|"OTLP trace"| TEMPO
    UM_C -.->|"OTLP trace"| TEMPO
    FD_C -.->|"OTLP trace"| TEMPO
    AQ_C -.->|"OTLP trace"| TEMPO
    AGENTS -.->|"log push"| LOKI
    PROM -.->|"scrape :9100 / :9200 / :9080"| AGENTS
    GRAF --> PROM
    GRAF --> LOKI
    GRAF --> TEMPO

    AQ_C -.->|"🚧"| EMB_C
    FD_C -.->|"🚧"| IMG_C

    classDef planned stroke-dasharray: 5 4;
    class EMB_C,IMG_C planned;
```

## Node

| Node | Chạy gì | Ghi chú |
|---|---|---|
| **VM1** — Oracle ARM, 10.0.0.64 | nginx, gateway, usermanagement, fishdex, aquahome, aquahome-worker, postgres, redis, promtail, node-exporter, cadvisor | Tất cả trên Docker network `fishlover_prod`. Chịu toàn bộ traffic người dùng |
| **VM2** — 10.0.0.94 | prometheus, loki, tempo, grafana | Chỉ nhận telemetry từ VM1 qua subnet private, không phục vụ người dùng |
| **VM3** 🚧 | embedding_service, image_search_service | Mới có scaffold thư mục, chưa có `main.py`/Dockerfile |
| **Cloudflare Pages** | SPA tĩnh đã build | Deploy tự động khi push, không qua Azure DevOps |
| **Cloudflare R2** | Ảnh loài, ảnh hồ, snapshot, video dự thi | Trình duyệt đọc/ghi trực tiếp bằng presigned URL |

## Điểm khác biệt quan trọng so với Level 2

Level 2 vẽ **ba database tách rời** theo service. Thực tế triển khai hiện nay chỉ có **một container `postgres`** chứa ba database, và **một container `redis`** dùng chung. Chúng tách được bất cứ lúc nào mà không phải đổi code, nhưng ở thời điểm này nếu đếm container đang chạy thì là **một**, không phải ba.

## Ghi chú phạm vi — không vẽ ở đây

- Quy trình CI/CD (Azure DevOps pipeline cho 4 service, Cloudflare Pages tự build từ Git).
- Backup database, gia hạn chứng chỉ TLS, quản lý secret (`.env` trên VM + Azure DevOps Library).
- Tiến độ sprint/epic — thuộc Notion, không thuộc tài liệu kiến trúc.
