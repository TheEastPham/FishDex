# docs/

Tài liệu cấp hệ thống của FishLover. Tài liệu riêng của một phần nằm cạnh phần đó: `BackEndProject/CLAUDE.md`, `BackEndProject/GLOSSARY.md`, `BackEndProject/docs/adr/`, `FrontEnd/CLAUDE.md`, `Pipeline/**/README.md`.

Repo này **public**: không đưa vào đây secret, kết quả audit bảo mật hay tài liệu của dự án khác.

| File | Là gì | Độ tin cậy | Kiểm lần cuối |
|---|---|---|---|
| [architecture/c4/](architecture/c4/README.md) | C4 L1 System Context, L2 Container, Deployment | Khớp code; nguồn đáng tin nhất về kiến trúc | 25/11/2026 |
| [architecture/reviews/2026-11-20-backend.html](architecture/reviews/2026-11-20-backend.html) | Architecture review backend 20/11 | **Lịch sử**: mọi mục đã làm xong, ô đầu trang ghi commit tương ứng. Không mô tả code hiện tại | 25/11/2026 |
| [roadmap/ai-release-plan.md](roadmap/ai-release-plan.md) | Kế hoạch phát hành AI v2.0 (R0–R7), ràng buộc, rủi ro, việc mở | Kế hoạch hiện hành; nhiều thứ trong đó chưa xây | 24/11/2026 |
| [roadmap/r0-data-analysis.md](roadmap/r0-data-analysis.md) | Phân tích độ phủ dữ liệu FishBase v24.07 làm nền cho R0 | Số liệu đo trên `data/fishbase-v24.07` của workspace | 24/11/2026 |
| [guides/fe-developer-guide.md](guides/fe-developer-guide.md) | Onboarding FE: stack, cấu trúc feature, auth, upload, quy ước | Đã sửa các chỗ lệch; chi tiết từng page có thể cũ dần | 26/11/2026 |

## Quy ước

- Thêm hoặc sửa tài liệu ở đây thì cập nhật bảng trên, nhất là cột **Kiểm lần cuối**.
- Tài liệu bị thay thế không để cạnh bản mới; chuyển ra `_archive/` của workspace (lịch sử git vẫn giữ).
- Một nội dung chỉ có một bản: ưu tiên Markdown (GitHub render được mermaid), không giữ song song bản HTML viết tay.
- BE developer guide sẽ được viết lại sau khi dọn codebase; trong lúc đó dùng `BackEndProject/CLAUDE.md`.
