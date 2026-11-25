---
status: accepted
---

# `CommonNames.SpecCode` không có khoá ngoại tới `Species`

Community species (mã từ `SpeciesCodes.CommunityStart` = 500.000 trở lên, thường là cá lai) chỉ tồn tại trong `SpeciesSnapshots`, nhưng vẫn được đặt tên bản ngữ (tiếng Việt) vì cá lai bán rất chạy. FK `CommonNames → Species` làm việc này ném lỗi 500, nên đã bỏ FK (migration `DropCommonNamesSpeciesFk`) cùng navigation `Species.CommonNames` / `CommonName.Species`.

Cố ý bỏ cả navigation chứ không chỉ drop constraint: EF Core không cho giữ quan hệ mà bỏ ràng buộc DB, và nếu chỉ drop FK trong migration thì model snapshot vẫn còn FK, lần `migrations add` sau sẽ tự thêm lại.

## Consequences

- Toàn vẹn tham chiếu do code giữ: `CommunityCommonNameRepository.SpeciesExistsAsync` kiểm tra bảng `Species` (dưới ngưỡng) hoặc `SpeciesSnapshots` đã duyệt (từ ngưỡng) trước khi ghi.
- Muốn đọc tên cùng loài phải truy vấn `db.CommonNames` theo `SpecCode`, không `Include`. Đừng thêm lại navigation.
- Xoá một loài khỏi `Species` (ETL) không còn tự chặn hay xoá `CommonNames` liên quan.
