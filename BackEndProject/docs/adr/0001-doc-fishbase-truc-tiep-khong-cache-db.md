---
status: accepted
---

# Species Profile đọc thẳng bảng FishBase, không có cache tầng DB

Species Profile truy vấn trực tiếp các bảng FishBase gốc bằng truy vấn gom theo lô; `SpeciesSnapshots` chỉ chứa Community species. Trước đây có lớp cache-aside (`DbSpeciesCache` + `FishBaseFlattener`) ghi snapshot vào DB ngay trong GET; nó mang rule chọn ảnh/ecology/tên lệch với trang detail và không bao giờ được làm mới, trong khi dữ liệu FishBase chỉ đổi khi chạy ETL nên cache không mua thêm được gì.

## Considered Options

- **Giữ cache snapshot cho FishBase species:** nhanh hơn trên lý thuyết, nhưng buộc phải có chiến lược làm mới sau mỗi lần ETL, và GET phải ghi DB (kéo theo `ChangeTracker.Clear()` và rủi ro DbContext dùng chung giữa các luồng).
- **Đọc thẳng (chọn):** summaries từ `1 + 8N` query xuống còn ~4; không có snapshot cũ để vá.

## Consequences

- Nếu hiệu năng đọc trở thành vấn đề, hãy cache ở tầng HTTP/memory theo `SpecCode + language` rồi xoá khi ETL chạy xong. Đừng đưa lại snapshot FishBase vào DB.
- Quyết định này không áp dụng cho Community species: chúng không có bảng gốc nào khác, `SpeciesSnapshots` là nơi lưu chính.
