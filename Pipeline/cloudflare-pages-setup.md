# FE Deploy — Cloudflare Pages

FE (aquahome-web) không cần Azure DevOps pipeline.
Cloudflare Pages tích hợp thẳng với GitHub — tự build + deploy khi push.

---

## Setup (1 lần)

1. Vào **dash.cloudflare.com** → **Workers & Pages** → **Create** → **Pages** → **Connect to Git**
2. Chọn repo `FishDex`, branch `release/prod-*` (hoặc `main`)
3. **Build settings:**

| Field | Value |
|-------|-------|
| Framework preset | Vite |
| Root directory | `FrontEnd/apps/aquahome-web` |
| Build command | `cd ../.. && npm install && cd apps/aquahome-web && npm run build` |
| Build output directory | `dist` |

> `cd ../..` là để lên `FrontEnd/` — chỗ đặt npm workspace root. Phải install từ đó
> thì `@fishlover/shared` mới link được, install trong `apps/aquahome-web` sẽ thiếu package.

4. **Environment variables** (Settings → Variables and secrets → Add):

| Name | Value PROD | Bắt buộc |
|------|-----------|----------|
| `VITE_GATEWAY_URL` | `https://api.fishlover.org` | ✅ |
| `VITE_AUTH_CLIENT_ID` | `aquahome-fe` | ✅ |
| `VITE_AUTH_REDIRECT_URI` | `https://fishlover.org/callback` | ✅ |
| `VITE_AUTH_POST_LOGOUT_URI` | `https://fishlover.org/` | ✅ |
| `VITE_CARTO_API_KEY` | key lấy tại https://carto.com/basemaps/apikey | ⬜ |

Để type là **Variable**, không phải Secret — Vite inline hết vào bundle JS nên không có
biến nào trong đây thật sự bí mật; đánh dấu Secret chỉ làm mất khả năng đọc lại giá trị
khi cần debug. Đừng đặt secret thật (DB password, private key...) ở đây.

Bộ tên biến này phải khớp `FrontEnd/apps/aquahome-web/.env.example` — đổi bên nào thì
sửa cả hai, sai tên thì build vẫn xanh nhưng app chạy sai config.

`VITE_CARTO_API_KEY` không bắt buộc: thiếu key thì map tự fallback sang OSM tiles
(nền sáng được invert bằng CSS), vẫn chạy nhưng không đúng tông dark. Xem
`packages/shared/src/lib/tileConfig.ts`.

5. **Save & Deploy** → Cloudflare tự build lần đầu

---

## Lưu ý: biến chỉ đọc lúc BUILD

Vite inline `VITE_*` thẳng vào bundle, không đọc lúc runtime. Kéo theo 2 hệ quả:

- Thêm/sửa biến xong **phải trigger deploy mới** (Retry deployment hoặc push commit).
  Deployment cũ giữ nguyên giá trị cũ.
- File `.env` scp lên Oracle VM (`Pipeline/OracleVM/shared/.env`) **không liên quan gì
  tới FE** — đó là biến cho docker-compose của BE. FE build trên hạ tầng Cloudflare,
  không build trên VM.

---

## Mỗi lần release

```bash
git checkout -b release/prod-v1
git push origin release/prod-v1
```

Cloudflare Pages tự detect push → build → deploy. Không tốn Azure DevOps minutes.

---

## Custom domain

Cloudflare Pages dashboard → **Custom domains** → thêm `fishlover.org`
SSL tự động, CDN toàn cầu, deploy ~30 giây.
