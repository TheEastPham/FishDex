# C4 model — FishLover

Mục lục các diagram C4. Mở bằng GitHub hoặc VS Code để thấy sơ đồ mermaid.

## 4 level cốt lõi

| Level | File | Nội dung |
|---|---|---|
| L1 System Context | [01-system-context.md](01-system-context.md) | Actor, "FishLover Platform" như một hộp, hệ thống ngoài (Groq, R2, YouTube, Resend, FishBase) |
| L2 Container | [02-container.md](02-container.md) | SPA, Gateway, 3 service BE, Worker, database, cache, AI services 🚧; kèm công nghệ và giao thức |
| L3 Component | chưa vẽ | Chỉ vẽ khi cần đào sâu một container cụ thể |
| L4 Code | không vẽ tay | Sinh từ IDE khi cần; bản đồ code backend có ở `generated/graphify/` của workspace |

## Diagram bổ sung

| Diagram | File | Nội dung |
|---|---|---|
| Deployment | [deployment.md](deployment.md) | Container chạy ở đâu: VM1 (backend PROD), VM2 (monitoring), VM3 (AI 🚧), Cloudflare Pages + R2 |
| System Landscape | chưa vẽ | FishLover chỉ có một hệ thống nên chưa cần |
| Dynamic | chưa vẽ | Vẽ khi cần giải thích một luồng khó (nộp bài dự thi, đăng nhập OAuth) |
