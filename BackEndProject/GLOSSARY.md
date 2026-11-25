# FishLover

Nền tảng tra cứu loài cá cảnh và theo dõi bể nuôi. Glossary này gom ngôn ngữ chung của các dịch vụ trong backend.

## Loài cá

**FishBase species**:
Loài lấy từ bộ dữ liệu FishBase, mã loài nằm dưới ngưỡng loài cộng đồng. Dữ liệu chỉ thay đổi khi chạy lại ETL.
_Avoid_: loài gốc, loài hệ thống

**Community species**:
Loài do người dùng gửi lên (thường là cá lai), có mã từ ngưỡng loài cộng đồng trở lên và phải được admin duyệt mới hiện ra công khai.
_Avoid_: hybrid species (khi muốn nói chung cả loài tự nhiên do cộng đồng thêm), user species

**Species Profile**:
Góc nhìn đọc đã lắp sẵn của một loài (FishBase hoặc cộng đồng): tên, ảnh, phân loại, sinh thái, môi trường nước. Mọi màn hình hiển thị loài đều đọc qua đây nên cùng một loài luôn ra cùng ảnh và cùng tên.
_Avoid_: species detail (đó chỉ là một dạng hiển thị), species cache

**Species Snapshot**:
Bản ghi phẳng lưu một Community species. Đây là nơi lưu chính của loài cộng đồng, không phải cache.
_Avoid_: snapshot cache, flattened species

**Thumbnail**:
Ảnh đại diện duy nhất của một loài, dùng trên mọi thẻ và danh sách.
_Avoid_: preferred image, best image

**Common name**:
Tên thường gọi của loài theo một ngôn ngữ. Có thể đến từ FishBase hoặc do người dùng đóng góp; chỉ tên đã duyệt mới được hiển thị.
_Avoid_: local name (đó là tên trong danh sách market), vernacular

## Upload

**Upload verification**:
Bước tin file thật sau khi người dùng tải thẳng lên storage. Link tải lên không chặn được dung lượng, nên hệ thống phải kiểm file thực tế trước khi nhận: chưa có file, quá cỡ, hoặc lệch với dung lượng người dùng đã khai thì bị từ chối, và file quá cỡ không bao giờ được để lại trên storage. Mỗi nghiệp vụ tự quy định giới hạn của mình và tự quyết định phản ứng khi bị từ chối.
_Avoid_: scan upload, validate upload
