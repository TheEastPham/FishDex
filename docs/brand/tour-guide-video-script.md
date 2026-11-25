# Kịch bản quay clip Tour Guide — fishlover.org

> **Mục tiêu:** giới thiệu tổng quát toàn trang, đủ sâu để người xem tự làm theo được.
> **Thời lượng:** 7:50. Lời thoại ~1.060 chữ — đọc ~150 chữ/phút.
> **Tông giọng:** chân thành, không hô hào. Xưng **mình**, gọi người xem là **bạn**. Đây là dự án cá nhân, đừng nói như startup.
> **Ngôn ngữ:** Tiếng Việt. Quay giao diện ở chế độ Tiếng Việt.

**Luồng kể chuyện:** khách vãng lai xem được gì → chạm trần phải đăng ký → dựng bể của riêng mình → để hệ thống nhắc mình chăm bể → mang bể ra khoe và đi thi → góp ngược lại cho cộng đồng.

---

## 0. Chuẩn bị trước khi quay

### Dữ liệu demo (thiếu là quay tới đâu cũng gặp màn hình rỗng)

| Cần | Chi tiết |
|---|---|
| Tài khoản "đã dùng lâu" | 2–3 hồ **nước ngọt** khác phong cách, mỗi hồ có ảnh bể đẹp, 5–8 loài, 3 nhắc nhở (1 quá hạn, 1 sắp tới, 1 đã xong), vài loài trong Cá yêu thích và Lịch sử tra cứu |
| Tài khoản "mới tinh" | Chưa có hồ nào — dùng cho đoạn 4–5 |
| 1 mã mời còn hiệu lực | Đăng ký bắt buộc có mã. **Che mã khi dựng** |
| 1 bể đã public + 1 bài dự thi đã duyệt | Cho đoạn 9–10 |
| Vài loài chưa có tên tiếng Việt trong `/market` | Cho đoạn 12. Hết thì đổi quốc gia khác |
| 1 hồ có cá từ nhiều châu lục | Cho đoạn 7 — bản đồ phải trải rộng mới đẹp |

### Kỹ thuật

- 1920×1080, trình duyệt full screen (F11), zoom 100%, ẩn bookmark bar, dùng cửa sổ ẩn danh.
- Bật highlight con trỏ chuột. Sau mỗi click, dừng 1 giây.
- **Quay từng đoạn thành file riêng.** Sai thì quay lại đúng đoạn đó, không quay lại cả bài.
- Dựng chặt: cut mọi khoảng chờ load, tua nhanh ×4 các đoạn điền form, dùng zoom-crop vào vùng cần chỉ thay vì cuộn chậm cả trang.
- Nhạc nền free-license (YouTube Audio Library). Video hướng dẫn cũng bị quét bản quyền như video dự thi.

### KHÔNG bấm vào (đang là trang trống)

`Nhật ký thông số` · `Bộ sưu tập` · `Trợ lý AI` · `Nhận diện cá` — chúng hiện trong sidebar suốt video, được giải thích ở đoạn 13.

### Sai sót cần tránh khi đọc lời thoại

- **Không có nhắc nhở "châm phân".** Hiện chỉ có **Thay nước** và **Vệ sinh lọc**. Châm phân nằm ở roadmap v1.1, chưa build.
- **Dữ liệu loài chỉ có nước ngọt.** ETL lọc `Fresh == 1` và `INCLUDE_BRACKISH = False`, nên trong FishDex không có loài nước mặn hay nước lợ nào. Nhưng **form tạo bể vẫn cho chọn đủ 3 loại nước** — ai chọn "Nước mặn" sẽ tạo ra một bể không thêm được con cá nào. Vì vậy: quay bể **nước ngọt**, và ở đoạn 5 phải nói rõ hai loại kia chưa có dữ liệu.
- **Không quay bể phong cách `Reef — San hô`.** Reef là hồ nước mặn, chọn nó rồi không tìm được cá nào để thả.

---

## Bảng thời lượng

| # | Đoạn | Giây | Cộng dồn |
|---|---|---|---|
| 1 | Mở đầu | 0:15 | 0:15 |
| 2 | Cá cảnh đang bán theo quốc gia | 0:45 | 1:00 |
| 3 | Tra cứu & hồ sơ loài | 1:05 | 2:05 |
| 4 | Bị chặn → Đăng ký → Dashboard | 0:45 | 2:50 |
| 5 | Tạo bể | 0:45 | 3:35 |
| 6 | Thêm cá & ảnh bể | 0:45 | 4:20 |
| 7 | Bản đồ gộp cả hồ | 0:12 | 4:32 |
| 8 | Nhắc nhở → Lịch trình → Thông báo | 0:55 | 5:27 |
| 9 | Public bể & quản lý bể đã public | 0:35 | 6:02 |
| 10 | Bể cộng đồng & Cuộc thi | 0:30 | 6:32 |
| 11 | Cá yêu thích · Lịch sử · Đổi ngôn ngữ | 0:20 | 6:52 |
| 12 | Đóng góp tên loài & Thêm loài | 0:35 | 7:27 |
| 13 | Đang làm & kết | 0:23 | 7:50 |

---

## Đoạn 1 — Mở đầu · 0:15

**Hình:** quay cận bể cá thật của bạn. Không quay màn hình.

> "Nuôi cá cảnh thì ai cũng từng mua một con về mà không biết nó tên gì, hợp nước thế nào, sống chung với con nào được. Mình làm fishlover.org để giải quyết đúng chuyện đó. Tám phút, mình dẫn bạn đi một vòng."

`[~35 chữ]` · **Chữ trên màn hình:** `fishlover.org`

---

## Đoạn 2 — Cá cảnh đang bán · 0:45

**Đường đi:** `fishlover.org` → tự vào `/market`

| Thao tác | Lời thoại |
|---|---|
| Trang vừa load, để 2s | "Gõ fishlover.org, chưa cần đăng nhập gì cả, bạn vào thẳng đây: những loài cá cảnh **đang thực sự được bán ở Việt Nam**." |
| Chỉ vào 3 ô thống kê | "Ba con số: bao nhiêu loài đang bán, bao nhiêu loài đã có tên tiếng Việt, và bao nhiêu loài **còn chờ đặt tên** — nhớ con số thứ ba, cuối video mình quay lại." |
| Cuộn qua vài thẻ, chỉ nhãn `Phổ biến` / `Hiếm` | "Mỗi thẻ cho biết loài đó dễ mua hay hiếm gặp ngoài tiệm: phổ biến, thỉnh thoảng, theo mùa, hay hiếm." |
| Chỉ nhãn `Hạn chế` / `Cấm nuôi` nếu có | "Và loài nào bị hạn chế hay cấm nuôi thì có nhãn cảnh báo." |
| Mở **Bộ lọc** → `dưới 5 cm` → **Xem N loài** | "Lọc được theo kích thước **lúc trưởng thành** — để tránh mua con bé xíu về rồi ba tháng sau nó chật bể." |
| **Xoá lọc** → đổi quốc gia trong dropdown | "Đổi quốc gia thì đổi danh sách. Nước nào mình chưa khảo sát thì trang nói thẳng là chưa có, chứ không bịa." |

`[~120 chữ]`

---

## Đoạn 3 — Tra cứu & hồ sơ loài · 1:05

**Đường đi:** `FishDex` → `Tra cứu & Bách khoa` → mở 1 loài

| Thao tác | Lời thoại |
|---|---|
| Gõ `betta` (gõ chậm, thấy rõ từng chữ) | "Sang mục tra cứu. Gõ tên khoa học hay tên thường gọi đều được, tiếng Việt hay tiếng Anh cũng được." |
| Chỉ vào bộ lọc **họ cá** | "Lọc thêm theo họ cá nếu bạn biết mình đang tìm nhóm nào." |
| Bấm **Xem hồ sơ** → trang chi tiết | "Đây là hồ sơ loài — phần mình đầu tư nhiều nhất." |
| Cuộn qua **Thông số nước** | "**Thông số nước**: nhiệt độ, pH, độ cứng dGH. Đây là số lấy từ dữ liệu FishBase chứ không phải mình tự nghĩ ra." |
| Cuộn qua **Sinh thái học** | "**Sinh thái**: kiểu ăn, tầng sống, và tập tính — loài này bơi theo đàn có tổ chức, tụ bầy, hay sống đơn độc. Biết trước khi mua thì đỡ mua nhầm về đánh nhau." |
| Cuộn tới **Bản đồ phân bổ** | "Bản đồ phân bố ngoài tự nhiên, theo toạ độ thật, kèm danh sách quốc gia." |
| Cuộn tới **Bảo tồn** | "Phần bảo tồn: trạng thái IUCN và mã CITES. Loài nào đang bị đe doạ thì trang cảnh báo rõ — mình muốn người nuôi cá biết chuyện đó." |
| Bấm **Thêm vào yêu thích** | "Thấy con nào ưng thì thả tim." |

`[~155 chữ]`

**Ghi chú:** chọn sẵn một loài **có ảnh đẹp và đủ dữ liệu ở cả 4 mục trên**. Loài thiếu dữ liệu sẽ ra một trang đầy chữ "Chưa rõ", quay lên nhìn rất tệ.

---

## Đoạn 4 — Bị chặn → Đăng ký → Dashboard · 0:45

**Đường đi:** (tài khoản chưa đăng nhập) bấm xem chi tiết → màn chặn → `/register` → `/dashboard`

| Thao tác | Lời thoại |
|---|---|
| Màn chặn hiện ra, để 2s | "Đến đây thì bạn gặp bức tường: xem đầy đủ hồ sơ loài cần có tài khoản. Nên mình đăng ký luôn." |
| Cut sang `/register`, chỉ 2 ô | "Cần một địa chỉ **Gmail** và một **mã mời**. Mã mời là vì mình đang mở dần chứ chưa mở đại trà — muốn giữ chất lượng dữ liệu và kiểm soát chi phí máy chủ. Cách xin mã mình để ở mô tả video." `[[CẦN ĐIỀN: cách lấy mã mời]]` |
| Bấm **Gửi mã xác thực**, cut sang bước 2 | "Bấm gửi, một mã xác thực về hộp thư của bạn." |
| Điền OTP + họ tên + mật khẩu (tua ×2) | "Nhập mã, điền họ tên, đặt mật khẩu — tối thiểu 8 ký tự, có chữ hoa, chữ thường và số." |
| Tạo xong → đăng nhập → `/dashboard`, để 3s | "Đăng nhập vào là thấy tổng quan: bao nhiêu hồ, tổng thể tích, đang nuôi bao nhiêu loài, bao nhiêu loài yêu thích." |

`[~115 chữ]`

**Ghi chú quay:** **che mã mời, OTP, mật khẩu, email thật khi dựng.** Cắt sạch đoạn chờ mail bằng jump cut.

---

## Đoạn 5 — Tạo bể · 0:45

**Đường đi:** `AquaHome` → `Hồ cá của tôi` (tài khoản mới, đang trống) → **Tạo hồ đầu tiên**

| Thao tác | Lời thoại |
|---|---|
| Màn hình trống | "Giờ tới phần chính: bể của bạn. Tài khoản mới thì trống trơn, mình tạo hồ đầu tiên." |
| Điền tên, mở dropdown **Loại nước** | "Đặt tên hồ, rồi chọn loại nước. Nói luôn cho bạn khỏi mất công: **hiện dữ liệu loài của mình đang là cá nước ngọt**. Nước mặn với nước lợ chọn được nhưng chưa có cá để thêm vào — mình đang bổ sung dần." |
| Chọn **Nước ngọt**, nhập kích thước | "Nên mình chọn nước ngọt. Nhập kích thước, thể tích tự tính ra." |
| Mở dropdown **Phong cách hồ**, cuộn chậm hết danh sách | "Rồi chọn phong cách: Nature kiểu Amano, Dutch nhiều cây màu, Iwagumi tối giản, Biotope mô phỏng môi trường tự nhiên, Blackwater nước đen Amazon, Community, Predator, Paludarium. Chọn cái gần bể bạn nhất." |
| Lưu | "Lưu lại là xong." |

`[~110 chữ]`

**Ghi chú:** trong lúc cuộn dropdown phong cách, **đừng dừng chuột ở `Reef — San hô`** và đừng đọc tên nó — Reef là hồ nước mặn, chưa có cá.

---

## Đoạn 6 — Thêm cá & ảnh bể · 0:45

**Đường đi:** `/fish` → 1 loài → **+ Thêm vào bể** → chi tiết hồ → **Ảnh bể**

| Thao tác | Lời thoại |
|---|---|
| Từ trang loài bấm **+ Thêm vào bể**, chọn hồ + số lượng | "Thêm cá vào bể thì làm ngược lại — từ trang loài, bấm **Thêm vào bể**, chọn hồ và số lượng." |
| Cut sang chi tiết hồ (tài khoản có sẵn dữ liệu), chỉ danh sách cá | "Trong hồ hiện ra danh sách từng loài, số lượng, quốc gia gốc. Bấm vào loài nào là nhảy thẳng sang hồ sơ loài đó." |
| Chỉ dãy thống kê phía trên | "Phía trên tự cộng: thể tích, số loài, tổng số cá." |
| Bấm **Thêm ảnh**, chọn file, tua nhanh lúc upload | "Chụp bể thì up thẳng lên đây. Ảnh được nén ngay trên máy bạn trước khi gửi, nên không tốn dung lượng mạng." |

`[~105 chữ]`

---

## Đoạn 7 — Bản đồ gộp cả hồ · 0:12

**Đường đi:** cuộn xuống bản đồ trong chi tiết hồ

> "Và đây là thứ mình thích nhất — bản đồ gộp toàn bộ cá trong bể. Bạn thấy ngay mình đang gom cá từ những vùng nào trên thế giới."

`[~30 chữ]`

**Ghi chú:** bắt buộc dùng hồ có cá từ **nhiều châu lục**. Hồ toàn cá Đông Nam Á thì các chấm dồn một góc, mất sạch hiệu ứng.

---

## Đoạn 8 — Nhắc nhở → Lịch trình → Thông báo · 0:55

**Đường đi:** chi tiết hồ → **Nhắc nhở** → `/tasks` → `Hồ sơ`

| Thao tác | Lời thoại |
|---|---|
| Cuộn tới **Nhắc nhở**, bấm **Thêm nhắc nhở** | "Nuôi cá thì việc dễ quên nhất là thay nước. Mỗi hồ có mục nhắc nhở riêng." |
| Chọn **Thay nước**, đặt giờ | "Chọn loại việc — **thay nước** hoặc **vệ sinh lọc** — rồi đặt giờ." |
| Chọn chu kỳ lặp | "Đặt chu kỳ lặp lại, ví dụ 7 ngày một lần." |
| Bấm **Xong** trên 1 nhắc nhở → hiện hộp lên lịch lần sau | "Làm xong thì tick, hệ thống hỏi luôn lần tiếp theo là khi nào." |
| Sang `/tasks` | "Còn muốn nhìn tất cả các hồ cùng lúc thì vào **Lịch trình**: **quá hạn**, **sắp tới**, **đã xong**. Sáng dậy mở lên là biết hôm nay phải làm gì." |
| Sang `Hồ sơ` → bật **Thông báo đẩy** | "Nhưng nhắc nhở mà phải tự mở web xem thì cũng bằng thừa — nên vào **Hồ sơ** bật **thông báo đẩy**, đến hẹn là điện thoại báo." |
| Chỉ vào dòng hướng dẫn iOS | "Riêng iPhone: mở trang này bằng **Safari**, bấm nút Chia sẻ, chọn **Thêm vào màn hình chính**, rồi mở lại app từ đó mới bật được. Đây là giới hạn của iOS chứ không phải lỗi." |

`[~150 chữ]`

---

## Đoạn 9 — Public bể & quản lý bể đã public · 0:35

**Đường đi:** chi tiết hồ → **Public bể** → `/my-published-tanks`

| Thao tác | Lời thoại |
|---|---|
| Bấm **Public bể** | "Bể ưng rồi thì mang ra khoe." |
| Modal hiện ra, chỉ 2 lựa chọn | "Bạn chọn **tạo bản ghi nhớ mới** — như chụp lại bể ở thời điểm này, mỗi hồ giữ tối đa 5 bản — hoặc **ghi đè bản cũ**, giữ nguyên link và số lượt thích, chỉ làm mới nội dung." |
| Chọn ảnh bìa → **Public bể** → bấm **Copy** | "Chọn ảnh bìa, public. Xong là có ngay một link riêng gửi bạn bè." |
| Sang **Bể đã public của tôi** | "Các bể đã public quản lý ở đây — copy lại link, hoặc gỡ public nếu đổi ý. Lưu ý gỡ rồi thì lượt thích mất." |

`[~95 chữ]`

---

## Đoạn 10 — Bể cộng đồng & Cuộc thi · 0:30

**Đường đi:** `/public/tanks` → `/contests`

| Thao tác | Lời thoại |
|---|---|
| Mở `/public/tanks`, cuộn qua vài bể | "Bể của mọi người nằm ở **Bể cộng đồng**." |
| Lọc theo **phong cách**, đổi **Nhiều tim nhất** | "Lọc theo phong cách, hoặc chỉ xem bể đã dự thi và bể đoạt giải. Sắp xếp theo mới nhất hay nhiều tim nhất." |
| Mở 1 bể, cuộn qua danh sách loài | "Vào từng bể xem ảnh, xem người ta nuôi những loài gì." |
| Sang `/contests`, dừng ở **Thể lệ chung** | "Và bể đã public chính là thứ bạn dùng để **dự thi**. Video phải quay ngang, dài 2 đến 5 phút, và **tuyệt đối không dùng nhạc bản quyền** — xếp hạng tính theo lượt xem YouTube, dính bản quyền là mất trắng cơ hội." |
| Cuộn xuống **Bảng xếp hạng** | "Bài được duyệt xong thì lên bảng xếp hạng. Mỗi cuộc thi có cơ cấu giải và nhà tài trợ riêng." |

`[~110 chữ]`

---

## Đoạn 11 — Cá yêu thích · Lịch sử · Ngôn ngữ · 0:20

| Thao tác | Lời thoại |
|---|---|
| Sang **Cá yêu thích** | "Mấy con bạn thả tim lúc nãy nằm ở đây." |
| Sang **Lịch sử tra cứu** | "Còn đây là những loài bạn vừa xem gần nhất — tiện lúc đang so sánh vài con trước khi quyết mua." |
| Bấm nút Globe trên thanh trên, đổi `English` rồi đổi lại | "Và toàn bộ trang có cả tiếng Anh, đổi ở đây." |

`[~55 chữ]`

---

## Đoạn 12 — Đóng góp tên loài & Thêm loài · 0:35

**Đường đi:** `/market` → thẻ **Chưa có tên Tiếng Việt** → `/submit-species` → `Đóng góp của tôi`

| Thao tác | Lời thoại |
|---|---|
| Về `/market`, lọc **Tên bản ngữ = Chưa có** | "Nhớ con số 'chờ đặt tên' lúc đầu chứ? Đây là chỗ bạn giúp được." |
| Rê chuột vào 1 thẻ | "Những loài này có trong dữ liệu khoa học nhưng chưa ai điền tên tiếng Việt. Mà ngoài tiệm thì người ta gọi tên tiếng Việt chứ có ai gọi tên Latin đâu." |
| Bấm **Bổ sung tên địa phương**, gõ tên, gửi | "Bạn biết dân trong nghề gọi nó là gì thì điền vào. Duyệt xong là hiện cho tất cả mọi người." |
| Sang **Thêm loài** | "Còn tìm mãi không thấy loài mình đang nuôi thì vào **Thêm loài** — điền tên, họ, thông số nước, kèm ảnh." |
| Sang **Đóng góp của tôi** | "Theo dõi ở **Đóng góp của tôi**: chờ duyệt, đã duyệt, hay bị từ chối — bị từ chối thì có lý do, sửa lại gửi tiếp được." |

`[~120 chữ]`

**Ghi chú:** đây là đoạn dễ mất chất nhất. Nói bằng giọng nhờ vả thật lòng, đừng đọc như lời kêu gọi marketing.

---

## Đoạn 13 — Đang làm & kết · 0:23

**Hình:** rê chuột dọc sidebar (không bấm), rồi cut về bể cá thật.

> "Mấy mục còn lại trong menu — nhận diện cá bằng ảnh, trợ lý AI, nhật ký thông số — mình đang làm, bấm vào còn trống là bình thường. Lộ trình mình ghi thật ở trang Bài viết, cái gì xong cái gì chưa đều có.
>
> Còn lại thì dùng được hết rồi. fishlover.org. Mình làm cái này vì mình cũng nuôi cá — bạn thử đi, thấy chỗ nào dở thì nói mình biết nhé. Cảm ơn bạn đã xem hết."

`[~85 chữ]` · **Chữ cuối:** `fishlover.org` · `facebook.com/FishLover` · `youtube.com/@FishLoverOrg`

---

## Short tách riêng (dựng lại từ file gốc từng đoạn)

| Short | Nội dung | Nguồn | Thời lượng |
|---|---|---|---|
| 1 | **Ba lỗi khiến video dự thi bị loại** — quay ngang, 2–5 phút, ≤500MB, không nhạc bản quyền, phải public bể trước | Đoạn 10 + quay bổ sung form nộp bài | 60s |
| 2 | **Bật thông báo trên iPhone** — Safari → Chia sẻ → Thêm vào màn hình chính | Đoạn 8 | 40s |
| 3 | **Tạo hồ cá đầu tiên** | Đoạn 5 + 6 | 60s |
| 4 | **Loài này chưa có tên tiếng Việt — bạn biết nó gọi là gì không?** | Đoạn 12 | 45s |
| 5 | **Xem cá cảnh nào đang bán ở Việt Nam** | Đoạn 2 | 45s |

---

## Checklist trước khi đăng

- [ ] Bấm đồng hồ: bản dựng cuối **≤ 8:00**. Quá thì cắt đoạn 11 trước, rồi tới đoạn 9
- [ ] Đã che: mã mời, OTP, mật khẩu, email thật, thông tin cá nhân lọt khung hình
- [ ] Không có cảnh nào bấm vào 4 trang chưa làm xong
- [ ] Không nói "châm phân" — chưa có tính năng này
- [ ] Nhạc nền free-license, đã ghi credit trong mô tả
- [ ] Mô tả video có: cách xin mã mời, link Facebook + YouTube, timestamp 13 đoạn
- [ ] Có phụ đề (nhiều người xem tắt tiếng)
- [ ] Xem lại trên **điện thoại** — chữ trên màn hình có đọc được không
