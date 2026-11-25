# Icon sidebar The FishLover

Nguồn: `lucide-react` v0.460.0 — bộ icon app đang dùng nguyên bản, chưa custom gì.
Tất cả SVG đều 24x24, `fill="none"`, `stroke="currentColor"`, `stroke-width="2"`,
`stroke-linecap/linejoin="round"` — giữ nguyên các thuộc tính này khi sửa để icon vẫn đổi màu theo theme.

Mở `preview.html` bằng browser để xem toàn bộ icon trên nền tối giống app.

| File | Component (code) | Dùng ở | Vị trí |
| --- | --- | --- | --- |
| `fish.svg` | `Fish` | Logo The FishLover / nhóm FishDex / Thêm loài | Logo + Sidebar |
| `layout-dashboard.svg` | `LayoutDashboard` | Tổng quan (nhóm) / Tổng hợp | Sidebar |
| `house.svg` | `Home` | AquaHome (nhóm) | Sidebar |
| `store.svg` | `Store` | Cá cảnh theo quốc gia / Quản lý danh sách cá bán | Sidebar |
| `search.svg` | `Search` | Tra cứu & Bách khoa | Sidebar |
| `heart.svg` | `Heart` | Cá yêu thích | Sidebar |
| `history.svg` | `History` | Lịch sử tra cứu | Sidebar |
| `book-open.svg` | `BookOpen` | Bộ sưu tập / Đóng góp của tôi | Sidebar |
| `users.svg` | `Users` | Cộng đồng (nhóm) / Duyệt đóng góp | Sidebar |
| `waves.svg` | `Waves` | Bể cộng đồng | Sidebar |
| `trophy.svg` | `Trophy` | Cuộc thi / Quản lý cuộc thi | Sidebar |
| `globe.svg` | `Globe` | Bể đã public của tôi (+ chọn ngôn ngữ ở header) | Sidebar |
| `settings.svg` | `Settings` | Tiện ích (nhóm) | Sidebar |
| `chevron-down.svg` | `ChevronDown` | Mũi tên mở/đóng nhóm | Sidebar |
| `chevron-right.svg` | `ChevronRight` | Mũi tên nhóm con | Sidebar |

## Ghi chú cho người sửa icon

- Icon hiện tại là bộ line-icon chung của Lucide, chưa mang đặc trưng cá cảnh / AquaHome.
- Nếu vẽ lại: giữ khung 24x24, nét 2px, bo tròn đầu nét để đồng bộ với các icon còn lại.
- Đừng gắn màu cứng (`fill`/`stroke` bằng mã hex) — app tô màu bằng CSS `currentColor`.
- Sidebar render icon ở 16px (menu con) và 18px (menu cha), nên chi tiết quá nhỏ sẽ bị bết.
- Logo góc trên bên trái trong ảnh = `fish.svg` đặt trong ô bo góc gradient xanh, không phải file riêng.
- Bộ logo thương hiệu nằm riêng ở `FrontEnd/apps/aquahome-web/public/brand/`, không gom vào đây.
