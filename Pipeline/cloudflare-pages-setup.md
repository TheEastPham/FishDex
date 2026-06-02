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

4. **Environment variables** (thêm trong Cloudflare Pages dashboard):

```
VITE_AUTH_ISSUER          = https://api.yourdomain.com
VITE_AUTH_CLIENT_ID       = aquahome-web
VITE_AUTH_REDIRECT_URI    = https://app.yourdomain.com/callback
VITE_AUTH_POST_LOGOUT_URI = https://app.yourdomain.com
VITE_AQUAHOME_API_URL     = https://api.yourdomain.com
VITE_FISHDEX_API_URL      = https://api.yourdomain.com
```

5. **Save & Deploy** → Cloudflare tự build lần đầu

---

## Mỗi lần release

```bash
git checkout -b release/prod-v1
git push origin release/prod-v1
```

Cloudflare Pages tự detect push → build → deploy. Không tốn Azure DevOps minutes.

---

## Custom domain

Cloudflare Pages dashboard → **Custom domains** → thêm `app.yourdomain.com`
SSL tự động, CDN toàn cầu, deploy ~30 giây.
