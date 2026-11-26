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

## Cuộc thi

**Contest entry** (bài dự thi):
Một video về một bể đã đăng công khai, gửi vào một cuộc thi. Mỗi bể chỉ có một bài còn hiệu lực trong một cuộc thi.
_Avoid_: submission, bài nộp

**Active entry** (bài còn hiệu lực):
Bài chưa bị từ chối và chưa hỏng. Chỉ bài còn hiệu lực mới tính vào trần số bài của một người và chặn nộp lại cùng một bể.
_Avoid_: active (từ này dành cho cuộc thi đang mở)

**Staging video**:
Bản video người dự thi tải lên kho của hệ thống. Chỉ giữ cho tới khi video đã lên YouTube hoặc bài không đi tiếp được; không bao giờ là bản lưu lâu dài.
_Avoid_: video R2, video gốc

**Upload queue** (hàng chờ đăng YouTube):
Các bài đã được xác nhận file và đang chờ hệ thống đăng lên YouTube. Người dự thi không phải làm gì thêm; hết lượt trong ngày thì bài đợi sang ngày sau.
_Avoid_: retry, xác nhận lại (khi nói về việc chờ lượt)

**YouTube upload allowance** (lượt đăng YouTube):
Số video hệ thống được đăng lên kênh trong một ngày, tính theo giờ Pacific (lúc YouTube làm mới hạn mức). Có trần chung cho cả hệ thống và trần riêng cho mỗi người; lượt đã dùng không được trả lại kể cả khi đăng hỏng.
_Avoid_: quota (từ này dành cho hạn mức theo role)

**Rejected** (bị từ chối):
Ban tổ chức quyết định không nhận bài vì nội dung. Chỉ người duyệt mới từ chối bài.
_Avoid_: dùng "từ chối" cho lỗi của hệ thống

**Failed** (không đăng được):
Bài dừng lại vì hệ thống không đưa được video lên: file thiếu hoặc lệch dung lượng, YouTube không nhận, hoặc để quá lâu không xác nhận. Không phải đánh giá về nội dung; người dự thi nộp lại được.
_Avoid_: rejected, bị từ chối

**Finalize** (trao giải):
Gán hạng giải cho các bài đã công khai rồi kết thúc cuộc thi. Cuộc thi chỉ kết thúc được khi không còn bài nào trong hàng chờ hay đang chờ duyệt.
_Avoid_: close (kết thúc mà không nói tới trao giải)
