---
status: accepted
---

# Giữ Generic Repository cho Aquarium, không gom thành aggregate store

Review kiến trúc 20/11/2026 đề xuất thay `GenericRepository<T>` (lưu ngay mỗi lần gọi) bằng một `AquariumStore` load theo owner kèm Fish và commit một lần. Quyết định: không làm. Phạm vi nhỏ (chỉ `Aquarium` và `UserFavorite` dùng generic repo, `AquariumService` ~134 dòng), chưa có test bao `AquariumService` nên đổi thời điểm commit là đổi hành vi khi lỗi giữa chừng mà không có gì bắt hồi quy, và lợi ích chính của đề xuất (bug `FishCount = 0`) sửa được bằng một query có `Include`.

Bug đó đã được sửa riêng: `GetByIdAndUserWithFishAsync` cho đường đọc, còn đường ghi vẫn dùng `GetByIdAndUserAsync` vì `UpdateAsync` của repo gọi `DbSet.Update` và sẽ đánh dấu cả đồ thị cá là Modified.

## Consequences

- Ownership check (`GetByIdAndUserAsync`) tiếp tục lặp ở từng method của service; chấp nhận được, có thể rút bằng helper trong service nếu cần.
- Điều kiện mở lại: nếu xuất hiện thao tác nhiều bước cần atomic (ví dụ thêm cá + cập nhật số liệu bể phải cùng thành công hoặc cùng thất bại), hãy viết test cho `AquariumService` trước rồi mới đề xuất lại.
