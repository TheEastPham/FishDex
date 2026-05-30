# Yêu cầu hỗ trợ từ Team Backend (API Tìm kiếm)

**Vấn đề:** 
Hiện tại, API tìm kiếm loài cá (`SpeciesSearchResult`) trả về cho Frontend (FE) mới chỉ có các trường text (tên khoa học, họ, chi...). Màn hình Search của FE đang phải sử dụng ảnh giả lập (placeholder gradient) vì chưa có đường dẫn hình ảnh thực tế, khiến trải nghiệm người dùng chưa được trực quan.

**Yêu cầu:** 
Cần BE bổ sung thêm đường dẫn ảnh đại diện (avatar) của loài cá vào danh sách kết quả tìm kiếm. Dữ liệu ảnh này được lấy từ `SystemImage` với điều kiện là ảnh được ưu tiên (`PicPreferred == true`).

---

## Các đầu việc cần Backend Team (BE) xử lý:

### 1. Cập nhật DTO trả về
- Tìm đến class DTO chứa kết quả trả về của danh sách tìm kiếm (tương ứng với `SpeciesSearchResult` dưới FE).
- Bổ sung thêm property mới, ví dụ: `public string? ImageUrl { get; set; }`.

### 2. Cập nhật logic truy vấn dữ liệu (Query/Mapping)
- Trong logic lấy dữ liệu (Repository / Query Handler), hãy join/include thêm thông tin từ bảng `SystemImage`.
- **Điều kiện lấy ảnh:** Lấy ảnh thuộc về loài cá đó và có trường `PicPreferred == true`.
- Nếu loài cá có nhiều ảnh `PicPreferred == true`, BE có thể lấy ảnh đầu tiên.
- Nếu không có ảnh nào thỏa mãn, trả về `null` (FE sẽ tự động hiển thị ảnh placeholder).

### 3. Thông báo cho FE sau khi hoàn tất
- Sau khi API được cập nhật và deploy/merge, BE vui lòng thông báo lại để FE tiến hành:
  - Cập nhật file `species.ts` (thêm field `imageUrl?: string | null;`).
  - Hiển thị ảnh thực tế lên thẻ `SpeciesCard.tsx` thay vì dùng gradient mặc định.
