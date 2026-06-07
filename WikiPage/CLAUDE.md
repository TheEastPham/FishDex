# WikiPage — FishLover Showcase Site

## ĐỌC TRƯỚC KHI LÀM BẤT CỨ ĐIỀU GÌ

### Đây là dự án gì?

Static showcase site tại `WikiPage/` — giới thiệu tổng quan FishLover platform cho người chơi cá và developer muốn đóng góp.

**Không phải:** documentation site, API reference, hay FrontEnd app.  
**Không cần:** auth, API calls, backend running.

---

## Nguồn gốc dự án — BẮT BUỘC PHẢI HIỂU

Đây là **personal project** — không phải startup, không gọi vốn, không có đội ngũ.

- Owner sinh ra ở làng quê miền Bắc Việt Nam, 199x. Tuổi thơ gắn với mương nước đầy cá, chum sành nuôi cá lia thia của anh trai.
- Lớn lên đi làm, nuôi cá để bớt cô đơn khi sống xa nhà.
- Khi làm cha, muốn nuôi cá đúng cách và an toàn — nhưng dữ liệu rải rác, không nhất quán, không có tiếng Việt. FishLover ra đời từ sự khó chịu thực tế đó.

**Tone & Voice:** Chân thành, mộc mạc, tỉ mỉ, tôn trọng tự nhiên.  
**Tuyệt đối cấm:** buzzword, corporate-speak, startup jargon.  
**Tuyệt đối không tự bịa:** Philosophy, Mission Statement — TBD, chờ owner định nghĩa.

---

## Ba tôn chỉ (phải phản ánh trong mọi nội dung)

1. **Tôn trọng Khoa học** — Dữ liệu từ FishBase (file `.parquet`). Không bịa, không đoán.
2. **Tôn trọng Tự nhiên** — Cảnh báo rõ ràng cho loài trong Sách Đỏ.
3. **Bảo vệ Con người** — Cảnh báo nghiêm ngặt loài nguy hiểm, đặc biệt với trẻ em.

---

## Boundaries — CRITICAL

- `WikiPage/` hoàn toàn độc lập. Không import từ `FrontEnd/` hay `BackEndProject/`.
- Chỉ commit trong `WikiPage/`. Không đụng thư mục khác.
- Không gọi API thực, không có auth.

---

## Audience

| Đối tượng | Mô tả |
|---|---|
| **Aquarists (chính)** | Người nuôi cá, muốn dữ liệu chính xác, nuôi cá an toàn và có khoa học |
| **Contributors (phụ)** | Developer muốn **đóng góp** cho dự án — code, dữ liệu, phản hồi |

**Lưu ý quan trọng:** Developer ở đây = người muốn **đóng góp**, KHÔNG phải người dùng API thương mại.  
API docs → Swagger. Không liệt kê endpoint, không viết auth flow, không code sample trên WikiPage.

---

## Tech Stack

```
Astro ^5.0.0   — Static site generator (output: 'static')
Vanilla CSS    — Không Tailwind, không CSS framework
TypeScript     — Chỉ trong frontmatter .astro
Deploy         — GitHub Actions → Azure Static Web Apps Free tier
```

Chạy local:
```bash
npm install
npm run dev      # http://localhost:4321
npm run build    # output → dist/
```

---

## Cấu trúc file

```
WikiPage/
├── public/favicon.svg
├── src/
│   ├── styles/global.css          ← CSS variables + base (sửa ở đây trước)
│   ├── layouts/BaseLayout.astro   ← HTML shell chung
│   ├── components/
│   │   ├── Nav.astro
│   │   └── Footer.astro
│   └── pages/
│       ├── index.astro            /
│       ├── about/
│       │   ├── index.astro        /about
│       │   ├── philosophy.astro   (TBD — không tự generate)
│       │   └── story.astro        (đã viết từ context owner)
│       ├── products/
│       │   ├── index.astro        /products
│       │   ├── fishdex.astro
│       │   ├── aquahome.astro
│       │   └── api.astro          (contribution + link Swagger, không có endpoint listing)
│       ├── tech.astro             /tech
│       └── roadmap.astro          /roadmap
├── astro.config.mjs
├── package.json
└── CLAUDE.md   ← file này
```

---

## Color Palette (khóa — giống FrontEnd)

| Vai trò | Hex | CSS var |
|---|---|---|
| Base / Background | `#0F172A` | `--c-base` |
| Surface (card) | `#1E293B` | `--c-surface` |
| Surface hover | `#263348` | `--c-surface-2` |
| Surface tối | `#172033` | `--c-surface-3` |
| Thiên nhiên / Teal | `#10B981` | `--c-nature` |
| AI / Cyan | `#06B6D4` | `--c-ai` |
| Action chính | `#0EA5E9` | `--c-primary` |

Không hardcode hex mới. Dùng CSS variables.

---

## Nội dung còn thiếu (TODO của owner)

| Trang | Thiếu gì |
|---|---|
| Tất cả | **Mission Statement chính thức** — owner chưa định nghĩa |
| `/about/philosophy` | Nội dung triết lý — TBD |
| `/products/fishdex` | Screenshots, demo link |
| `/products/aquahome` | Screenshots, link app live |
| `/products/api` | Link Swagger thực tế khi deploy |
| `/tech` | Sơ đồ kiến trúc |
| Toàn bộ | Link GitHub repo (khi public) |

---

## Thông tin kỹ thuật FishDex (để viết content)

- Dữ liệu loài: FishBase `.parquet` — KHÔNG phải CSV
- ~35,000 loài cá, bắt đầu với nước ngọt (tương lai: mặn/lợ)
- 3 service: UserManagement, FishDex (PostgreSQL + pgvector), AquaHome
- Tìm kiếm ngữ nghĩa: pgvector trên PostgreSQL 16
- Backend: .NET 9, Ocelot API Gateway, Autofac, OpenTelemetry + Prometheus
- Auth: OpenIddict, OAuth2 PKCE
- Ảnh: MinIO (presigned URL)
- Tương lai: AI Image Search, AI Q&A advisor, co-op với trại cá và viện nghiên cứu

---

## Deploy (chưa cấu hình)

Target: Azure Static Web Apps Free tier.  
Pipeline sẽ ở: `WikiPage/.github/workflows/azure-static-web-apps.yml` (chưa tạo).  
Cập nhật `astro.config.mjs → site` trước khi deploy.
