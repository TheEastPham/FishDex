---
status: accepted
---

# App mobile dùng Expo trong repo này; dịch vụ ML của AI tách repo riêng

Phase 2.0 thêm app mobile và phần AI, cả hai deploy độc lập với web và backend. Quyết định chia theo công nghệ và đích chạy, không theo tên tính năng:

- **App mobile: React Native + Expo, nằm ở `FrontEnd/apps/mobile`.** App dùng lại `@fishlover/shared` (API client, làm mới token, PKCE, kiểu dữ liệu, i18n 4 ngôn ngữ — phần đã có test). EAS Build dịch cả iOS lẫn Android trên cloud (máy dev là Windows, không build iOS tại chỗ được), EAS Submit đẩy lên hai store, EAS Update sửa lỗi phần JS không chờ duyệt.
- **Dịch vụ ML (embedding :8000, image search :8001 trên VM3, script tiền xử lý `Pipeline/Preprocessor`, notebook đánh giá): repo riêng `FishLover-AI`**, có thể private. Khác ngôn ngữ (Python), khác máy, không dùng chung code với .NET.
- **Phần tích hợp AI trong .NET** (`ILlmProvider`, endpoint RAG, entity `SpeciesChunk`/`ClipEmbedding` + migration pgvector, quota `AiQaPerDay`) **ở lại repo này** vì nằm trong chính các service và database hiện có. Hai repo chỉ nói chuyện qua HTTP qua gateway.

"Deploy độc lập" không phải lý do để tách repo: pipeline đã lọc theo thư mục, build mobile cũng lọc được theo `FrontEnd/apps/mobile/**`.

## Considered Options

- **Mobile ở repo riêng.** Phải đóng gói `@fishlover/shared` thành package có phiên bản và giữ hai repo khớp nhau — nặng cho một người làm.
- **Flutter.** Hiệu năng tốt nhưng viết lại toàn bộ client bằng Dart, không dùng lại được gì.
- **Native riêng (Swift + Kotlin).** Nhanh nhất nhưng hai codebase, gấp đôi công sức và chỗ sinh lỗi.

## Consequences

- API phải giữ tương thích ngược: app đã cài không cập nhật cùng lúc với backend. Giữ đúng tiền tố `/v1` của gateway, đổi kiểu phá vỡ thì ra `/v2`.
- Trước khi viết app: bật validate audience (ADR 0004, checklist prod #4); sửa discovery OIDC đang trả host nội bộ (`usermanagement-api:8080`) vì AppAuth đọc nó; đăng ký client OAuth cho mobile (redirect dạng `fishlover://callback`); tách phần chỉ web dùng (Tailwind `cn()`, `sessionStorage`) khỏi `@fishlover/shared` — mobile lưu token bằng SecureStore; chọn kênh push mobile (Expo Push hoặc FCM/APNs) đặt sau `IWebPushNotifier`.
- Chi phí: Expo Free (15 build iOS + 15 Android/tháng, 1.000 người dùng EAS Update) đủ cho giai đoạn đầu; tài khoản Apple Developer 99 USD/năm, Google Play 25 USD một lần.
- Khi tạo repo `FishLover-AI`: chuyển `Pipeline/Preprocessor` và scaffold `Pipeline/OracleVM/VM3/*_service` sang đó; phần hạ tầng VM3 (compose, README) có thể ở lại `Pipeline/OracleVM/VM3`.

## Hướng sau này (chưa làm, ghi để không chọn thứ gì chặn đường)

- **Ảnh và video quản lý ngay trên máy, dùng được khi không có mạng.** Dựa trên `expo-media-library`, `expo-file-system`, `expo-sqlite`; có mạng thì đồng bộ lên R2. Mọi luồng upload phải chịu được gửi lại nhiều lần mà không sinh bản trùng (giữ kiểu presigned PUT → confirm như ảnh bể và video dự thi hiện nay).
- **Mô hình AI nhỏ chạy trên điện thoại.** Vẫn trong Expo, nhưng cần development build có thư viện native (họ llama.cpp, ExecuTorch hoặc Core ML), không chạy trong Expo Go. Máy tối thiểu là iPhone 12 (4 GB RAM) nên trần thực tế khoảng 1 tỷ tham số, nén 4-bit (~0,6–0,8 GB). Mô hình tải về khi người dùng bật tính năng, không đóng vào gói cài. Huấn luyện, thu gọn và xuất mô hình (GGUF / ExecuTorch / Core ML) làm trong repo `FishLover-AI`.
