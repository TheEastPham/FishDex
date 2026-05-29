# AquaHome FE

Frontend duy nhất của hệ thống FishLover — giao tiếp **chỉ với AquaHome Service**, không gọi trực tiếp FishDex/AI/ImageSearch (BFF pattern).

## Tech Stack

| Concern | Lựa chọn | Lý do |
|---------|----------|-------|
| Framework | React 19 | Ecosystem lớn, SSE streaming dễ (Story 5.4) |
| Build tool | Vite | Nhanh hơn CRA ~10×, HMR tức thì, CRA đã deprecated |
| Language | TypeScript | Type-safe, bắt lỗi sớm khi gọi API |
| Routing | React Router v7 | Standard cho React SPA |
| State | Zustand | Nhẹ hơn Redux, đủ cho scale hiện tại |
| HTTP | Axios | Interceptor dễ xử lý token refresh |
| Charts | Recharts | Story 5.2 — water parameter chart |
| Styling | Tailwind CSS | Utility-first, không cần setup nhiều |
| Components | shadcn/ui | Copy-paste components, Tailwind-based, không vendor lock-in |
| Map | react-leaflet + OpenStreetMap | Free, không API key, hỗ trợ lat/lng pin cho fish occurrence data |

## Folder Structure

```
FrontEnd/
├── public/
├── src/
│   ├── layouts/               # App Shell và các layout wrapper
│   │   └── AppShell.tsx       # Sidebar + <Outlet /> — bọc toàn bộ protected routes
│   ├── features/              # Feature-based — mỗi Story là 1 folder
│   │   ├── auth/              # Story 5.1: PKCE login, token, logout
│   │   ├── aquarium/          # Story 5.2: dashboard, fish inventory, water chart
│   │   ├── fish-search/       # Story 5.3: search bar, species card, add to aquarium
│   │   ├── ai-chat/           # Story 5.4: chat widget, SSE stream, source citation
│   │   └── image-search/      # Story 5.5: drag & drop upload, top-5 results
│   ├── components/            # Shared UI components (Button, Modal, Card...)
│   ├── hooks/                 # Shared hooks (useAuth, useDebounce...)
│   ├── lib/
│   │   ├── api/               # Axios instances + endpoint functions
│   │   └── auth/              # PKCE helpers, token storage
│   ├── types/                 # TypeScript types/interfaces dùng chung
│   ├── App.tsx
│   └── main.tsx
├── .env.example
├── index.html
├── vite.config.ts
├── tsconfig.json
└── package.json
```

## Setup & Run

```bash
cd FrontEnd

# Cài dependencies
npm install

# Copy env
cp .env.example .env.local

# Dev server (port 5173)
npm run dev

# Build production
npm run build
```

## Environment Variables

```env
# .env.example
VITE_AQUAHOME_API_URL=http://localhost:8082
VITE_AUTH_ISSUER=http://localhost:8080
VITE_AUTH_CLIENT_ID=aquahome-fe
VITE_AUTH_REDIRECT_URI=http://localhost:5173/callback
```

> Tất cả biến env của Vite phải có prefix `VITE_` mới được expose ra browser.

## Auth Flow — OAuth2 PKCE

```
Browser                UserManagement (port 8080)      AquaHome (port 8082)
   |                           |                              |
   |-- /login ---------------→ |                              |
   |← redirect + code -------- |                              |
   |-- /callback (code) -----→ |                              |
   |← access_token ----------- |                              |
   |                           |                              |
   |-- API call + Bearer -------------------------→           |
```

- **Access token**: lưu trong memory (JS variable) — không lưu localStorage, tránh XSS
- **Refresh token**: `sessionStorage` (key `_rt`) — mất khi đóng tab, user re-login mỗi session. TODO: upgrade lên BFF httpOnly cookie cho production
- **Token refresh**: Axios interceptor tự động retry khi nhận 401, deduplicate concurrent calls

## Stories & Dependencies

| Story | Mô tả | Backend cần | Status |
|-------|-------|-------------|--------|
| 5.1 Auth Flow | PKCE login / logout / refresh | UserManagement ✅ | Unblocked |
| 5.3 Fish Search UI | Search + species card | FishDex ✅ | Unblocked |
| 5.2 Aquarium Dashboard | CRUD bể + fish inventory + water chart | AquaHome (Epic 3) | Chờ Epic 3 |
| 5.4 AI Chat Widget | SSE streaming + source citation | AI Q&A (Epic 2) | Chờ Epic 2 |
| 5.5 Image Search UI | Drag & drop upload + top-5 results | Image Search (Epic 4) | Chờ Epic 4 |

## Azure DevOps Pipeline

Pipeline FE được trigger độc lập qua path filter — push code BE không trigger FE pipeline và ngược lại.

```yaml
# Pipeline/FE/azure-pipelines-fe.yml
trigger:
  branches:
    include:
      - main
  paths:
    include:
      - FrontEnd/**
```

## Design Decisions

**Tại sao Vite thay vì CRA?**
CRA đã không còn được maintain (last release 2022). Vite cold start ~300ms vs CRA ~30s trên project lớn. Ecosystem đã chuyển sang Vite.

**Tại sao Zustand thay vì Redux?**
Redux có boilerplate nặng không cần thiết ở scale này. Zustand ~1KB, API đơn giản, đủ manage auth state + aquarium state.

**Tại sao không dùng Next.js?**
AquaHome FE là SPA thuần — không cần SSR/SSG. Next.js thêm complexity (server components, routing conventions) không mang lại lợi ích ở đây. Vite + React Router đủ.

**Layout: App Shell với sidebar**
Toàn bộ protected routes (`/dashboard`, `/fish`, `/ai-chat`, `/image-search`) được bọc trong `AppShell` — sidebar cố định bên trái, content area bên phải. Login/Callback là standalone full-screen (không có sidebar). 4 feature sections ngang nhau về tầm quan trọng nên sidebar phù hợp hơn top navbar.

**Tại sao shadcn/ui?**
Tích hợp trực tiếp với Tailwind đã có sẵn. Copy-paste components (không bundle toàn bộ library), dễ customize, không vendor lock-in. Chuẩn thực tế cho React 2024-2026.

**Design reference: kaiadmin-lite**
Layout, màu sắc, card style lấy cảm hứng từ [kaiadmin-lite](https://themewagon.github.io/kaiadmin-lite/) nhưng implement bằng shadcn/ui + Tailwind (không dùng Bootstrap của template gốc để tránh conflict).

**Tại sao react-leaflet thay vì Google Maps / jsvectormap?**
- Google Maps: cần API key, có thể mất phí
- jsvectormap (dùng trong kaiadmin): chỉ hiển thị choropleth theo region/country — không hỗ trợ lat/lng pin marker, không phù hợp với fish occurrence data từ FishBase
- react-leaflet + OpenStreetMap: free, không API key, hỗ trợ đầy đủ lat/lng markers, plugin ecosystem phong phú

**Mobile porting strategy**
Kiến trúc tách biệt `lib/` (API, auth) + `store/` + `hooks/` khỏi UI layer. Khi port sang React Native: giữ nguyên toàn bộ logic, viết lại UI với React Native components + NativeWind. Map: thay react-leaflet bằng react-native-maps.
