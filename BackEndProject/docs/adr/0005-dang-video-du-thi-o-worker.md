---
status: accepted
---

# Đăng video dự thi lên YouTube ở Worker, không trong request xác nhận

Bước xác nhận bài dự thi từng tải video (tới 500 MB) từ R2 về rồi đẩy lên YouTube ngay trong request HTTP, trong khi FE chỉ chờ 10 giây và nginx 60 giây. Request bị cắt giữa chừng thì bài kẹt ở trạng thái đang xử lý, người dự thi không bấm xác nhận lại được và mất một lượt đăng trong ngày. Quyết định: xác nhận chỉ kiểm file trên storage rồi đưa bài vào hàng chờ; `AquaHome.Worker` lấy bài trong hàng chờ, giữ lượt YouTube ngay trước khi đăng, và đăng lần lượt từng bài. Hết lượt trong ngày thì bài nằm chờ sang ngày sau, người dự thi không phải làm gì.

## Considered Options

- Giữ việc đăng trong request, nâng timeout của FE và nginx cho riêng lệnh xác nhận, và không huỷ bước YouTube khi client ngắt. Ít sửa hơn nhưng request vẫn kéo dài vài phút, iPhone khoá màn hình giữa chừng là đứt, và process API phải kéo cả video qua đĩa.

## Consequences

- Chỉ chạy **một** instance Worker. Worker không khoá bài trước khi đăng; hai instance cùng chạy sẽ đăng trùng.
- Worker chết đúng lúc đang đăng thì bài vẫn ở hàng chờ và sẽ được đăng lại: có thể sinh một video unlisted trùng trên kênh và tốn thêm một lượt. Chấp nhận, vì mất bài của người dự thi tệ hơn.
- Trần lượt đăng mỗi người mỗi ngày do Worker áp lúc đăng; lúc nộp bài không còn chặn vì hết lượt.
- Worker cần credential YouTube và R2 (VM1 đã cấp sẵn cho job đồng bộ lượt xem và job dọn staging). API vẫn cần credential YouTube để công khai và xoá video khi admin duyệt hay từ chối.
