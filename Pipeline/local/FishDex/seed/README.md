# Seed lớp market — Việt Nam

Đây là **bootstrap một lần**, không phải nơi quản lý dữ liệu lâu dài.

Sau khi seed xong lần đầu, nguồn sự thật là DB. Thêm hoặc gỡ cá thì dùng màn
`/admin/market`, không sửa file ở đây rồi commit. Nội dung mà phải đi qua vòng
sửa code → commit → deploy là sai luồng, và sẽ thành nguồn sự thật thứ hai
cạnh tranh với chính admin UI của sản phẩm.

Lý do file vẫn nằm trong repo: để chạy lại được y hệt trên PROD, và để sau này
truy được vì sao một loài có trong danh sách. `TradedSpecies` chỉ lưu `Origin` và
`AddedBy`, không có cột nào giữ "con này vào vì tiệm X bán" — nên xuất xứ sống ở
đây và ở git history, không sống trong DB.

## Files

| File | Vai trò |
|---|---|
| `market-vn.csv` | Dữ liệu curate tay. **File duy nhất cần sửa.** |
| `build_market_seed.py` | Tra tên khoa học trong `species.parquet` để lấy SpecCode, sinh output |
| `market_vn_*.csv` | Output sinh ra, không commit |

Script **chỉ đọc** `parquetData/` và chỉ ghi vào chính thư mục `seed/`. Parquet là
dữ liệu gốc của ETL — hỏng là phải tải lại toàn bộ, nên đừng bao giờ thêm đường
ghi ngược vào đó.

Mở `market-vn.csv` bằng Excel thì vào Data → From Text/CSV rồi chọn UTF-8,
mở thẳng sẽ vỡ dấu tiếng Việt. Google Sheets thì mở trực tiếp được.

## Cột trong `market-vn.csv`

| Cột | Nghĩa |
|---|---|
| `tenBan` | Tên tiệm bán, tiếng Việt |
| `loai` | `loai` = loài FishBase · `laiTao` = loài lai tạo · `chuaTra` = chưa tra được |
| `tenKhoaHoc` | Tên khoa học **theo đúng cách FishBase đang viết**, không phải tên tiệm ghi |
| `loaiCha` | Chỉ dùng cho `laiTao`. Để trống nếu là lai khác loài, không có loài cha hợp lệ |
| `nguon` | Tiệm nào bán |
| `ghiChu` | Vì sao — nhất là khi không map được, để lần sau khỏi tra lại |

## Luật tách loài lai tạo

Danh mục tiệm bán **mặt hàng**, lớp market đếm **loài**. Kingaqua có 375 mặt hàng
nhưng chỉ khoảng 60 loài — riêng cá bảy màu đã ~50 mặt hàng. Nếu không có luật
dừng thì 375 mặt hàng đẻ ra 375 "loài", đúng thứ lớp market phải tránh.

Chỉ tách sang `laiTao` khi tên tiếng Việt **đứng độc lập**, không mang tên loài
cha làm gốc:

- tách: `Cá Ranchu`, `Cá Ping Pong`, `Cá ket` — không ai đi mua nói "cá vàng",
  họ nói thẳng tên dòng
- không tách: `Cá bảy màu Full Red`, `Cá thần tiên Koi`, `Cá mún mickey` — là tên
  cha + bổ nghĩa, người mua vẫn coi "cá mún" là một loại

Luật này chặn được chia nhỏ vô tận: Ranchu đầu lân, Ranchu Thái vẫn là
"Ranchu + bổ nghĩa" nên không tách tiếp.

Dòng `laiTao` **không có SpecCode**, nên không vào thẳng lớp market được. Chúng
phải đi qua luồng community species đã có sẵn (`SpeciesSnapshot`,
`DataSource = Community`), được cấp mã ≥ 500000, rồi mới thêm mã đó vào market.

## Hai điều đã kiểm và cần nhớ

**Trường `Aquarium` của FishBase không phải tín hiệu thị trường.** Nó là đánh giá
toàn cầu, thiên về bảo tồn. 11 loài tiệm Việt Nam đang bán bị nó gắn
`never/rarely` hoặc nước lợ nên ETL không nạp — trong đó có `Pethia padamya`
(diếc vẩy rồng Odessa, tiệm nào cũng có), `Peckoltia compta` (pleco L134),
`Poecilia wingei` (Endler), và `Tanichthys micagemmae` là **loài đặc hữu Việt Nam**.

Đừng nới `AQUARIUM_VALUES` để sửa: vùng `never/rarely` có 14.853 loài nước ngọt,
nới ra là ngập rác. Cách đúng là cho ETL nạp thêm một danh sách trắng SpecCode
đã chứng minh có bán — chính là output của pipeline này.

**Tên tiệm ghi hay là tên cũ.** `Puntius conchonius` → FishBase gọi
`Pethia conchonius`; `Sahyadria denisonii` → `Dawkinsia denisonii`;
`Epalzeorhynchos kalopterus` → đuôi `-um`; `Glossolepis incisus` → đuôi `-a`.
Không quy đổi thì loài có sẵn trong FishDex vẫn bị coi là không tìm thấy.
Cột `tenKhoaHoc` phải ghi theo FishBase, tên tiệm ghi thì để ở `ghiChu`.

## Nguồn

Crawl ngày 2026-08-09:

- <https://senaquatic.vn/ca-canh-sen-aquatic?q=collections:2358517&view=grid> — 45 mặt hàng, phần lớn có tên khoa học trong ngoặc
- <https://kingaqua.vn/ca-canh-pc204970.html> — 15 trang, 375 mặt hàng, không mặt hàng nào có tên khoa học
- <https://thuysinhtim.vn/ca-canh-tep-canh> — 24 mặt hàng, thiên về hàng hiếm
- <https://cacanhthienduc.com/ca-dia-dong-ca-nhieu-mau-sac-nhat-hoa-van-tren-than-an-tuong> — cá dĩa

Danh mục tiệm có sai sót: senaquatic ghi "Cá Xray Tetra (Paracheirodon innesi)"
— sai, `P. innesi` là cá neon còn X-ray tetra là `Pristella maxillaris`. Không
nuốt trọn dữ liệu tiệm.
