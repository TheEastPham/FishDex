# Yêu cầu hỗ trợ từ Team Backend (Tính năng Favorite / Thả tim cá)

**Vấn đề:** 
Frontend (FE) vừa cập nhật lại giao diện `SpeciesCard` (thẻ hiển thị thông tin loài cá) theo thiết kế mới, trong đó có bổ sung nút **Thả tim (Favorite)** để tăng tương tác. Hiện tại, nút này mới chỉ hoạt động hiệu ứng ở phía giao diện (bằng React State) chứ chưa thể lưu lại dữ liệu vĩnh viễn vì chưa có API hỗ trợ.

**Yêu cầu:** 
Cần BE thiết kế Database và cung cấp các API để quản lý danh sách các loài cá yêu thích (Favorite List) theo từng User.

---

## Các đầu việc cần Backend Team (BE) xử lý:

### 1. Thiết kế cơ sở dữ liệu
- Tạo mới một bảng lưu trữ quan hệ nhiều-nhiều giữa User và Species (ví dụ: `UserFavoriteSpecies`).
- Các trường cơ bản: `UserId` (từ Identity), `SpecCode` (hoặc SpeciesId tương ứng).

### 2. Cung cấp API Thêm / Bỏ yêu thích
- Cần một API (ví dụ: `POST /api/favorites/toggle` hoặc `POST /api/favorites/{specCode}`).
- Logic: Kiểm tra xem User hiện tại đã lưu loài cá này chưa. Nếu chưa thì Thêm (Insert). Nếu đã có rồi thì Xóa (Delete/Un-favorite).

### 3. Cung cấp API Lấy danh sách yêu thích
- Một API (ví dụ: `GET /api/favorites`) trả về danh sách các loài cá (`SpeciesSearchResult`) mà User hiện tại đã thả tim.

### 4. (Quan trọng) Cập nhật DTO của API SearchSpecies hiện tại
- Để FE hiển thị đúng trạng thái tim (đỏ/trắng) ngay từ lúc tìm kiếm, mong BE hãy join vào bảng Favorite (dựa trên UserId đang gửi Request, nếu có) và trả về thêm 1 field boolean trong class `SpeciesSearchResult`.
- Ví dụ: `public bool IsFavorite { get; set; }` (trả về `true` nếu User hiện tại đã lưu loài cá này).

### 5. Thông báo cho FE sau khi hoàn tất
- Sau khi BE deploy, vui lòng thông báo lại kèm Swagger URL / Postman collection để FE:
  - Cập nhật interface `SpeciesSearchResult` (thêm field `isFavorite`).
  - Gọi API thực tế khi người dùng bấm vào nút Trái tim.
