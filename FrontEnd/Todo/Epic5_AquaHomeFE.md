# 🖥️ Epic 5: AquaHome FE

> **Status:** In progress | **Priority:** Medium | **Phase:** 3–4
> **Stack:** React 19 + Vite + TypeScript

---

## 📐 Design Reference

- Diagram: `Planning&Design/Design/system-context.html` → Tab **"System Context"** (AquaHome FE là điểm tiếp xúc duy nhất với user)
- Roadmap: `Planning&Design/Planning/roadmap-option-b.html` → **Phase 3–4**

**Tại sao Epic này tồn tại:**
Theo Option B roadmap, client chỉ có một FE duy nhất giao tiếp với AquaHome Service. FE này bao gồm tất cả tính năng: quản lý bể cá, tìm kiếm loài, AI chat, nhận diện ảnh. Các service FE riêng cho FishDex/AI/ImageSearch là tương lai khi có thêm nhân lực.

**Key design decisions:**
- OAuth2 PKCE flow — không lưu client secret ở browser, token an toàn
- Token trong memory (access) + sessionStorage (refresh, dev) — upgrade lên BFF httpOnly cookie khi production
- FE chỉ gọi AquaHome Service (target architecture) — Story 5.3 hiện gọi FishDex trực tiếp, refactor qua AquaHome khi Epic 3 xong
- Phase cuối vì cần tất cả backend services sẵn sàng trước khi làm FE có giá trị

---

## Stories

### Story 5.1: Auth Flow ✅
- [x] OAuth2 PKCE login (redirect → UserManagement → callback)
- [x] Token storage (access token in memory + refresh token in sessionStorage)
- [x] Logout + token revocation (POST /connect/revoke)

### Story 5.2: Aquarium Dashboard
- [ ] List / create / edit aquariums
- [ ] Fish inventory per aquarium
- [ ] Water parameter chart (recharts hoặc chart.js)

### Story 5.3: Fish Search UI ✅
- [x] Search bar → FishDex species (debounced 400ms)
- [x] Species detail card (tên khoa học, common name badge, genus, family)
- [ ] Add fish to aquarium — UI sẵn sàng (button disabled), unblock khi Story 5.2 xong

### Story 5.4: AI Chat Widget
- [ ] Chat interface (input + message list)
- [ ] SSE streaming response
- [ ] Source citation display (chunk references)

### Story 5.5: Image Search UI
- [ ] Upload ảnh (drag & drop)
- [ ] Hiển thị top-5 kết quả với similarity score

### Story 5.6: Species Detail Page
- [ ] Route `/fish/:specCode` — navigate từ nút "View Profile" trên `SpeciesCard`
- [ ] Gọi `GET /api/species/{specCode}/detail?language=vi` — **đã có BE, phụ thuộc đã unlock**
- [ ] Gọi `GET /api/species/{specCode}/media` — cho full gallery
- [ ] Gọi `GET /api/species/{specCode}/occurrences` — cho map
- [ ] Hero section: preferred image + tên KH + common name
- [ ] So sánh đực/cái: dùng `maleImageUrl` + `femaleImageUrl` từ `/detail` response
- [ ] Occurrence map: **react-leaflet + OpenStreetMap**
- [ ] Section Ecology: feedingType, dietTroph, habitatZones
- [ ] Section Conservation: IUCN code/assessment, CITES, nhiệt độ/pH

> ✅ **Phụ thuộc đã sẵn sàng:** Story 1.4 + 1.5 Done. Detail endpoint mới (Story 1.6) đã deploy tại `http://localhost:8081`.

---

## 📡 API Contract — Story 5.6

### `GET /api/species/{specCode}/detail?language=vi`

```typescript
interface SpeciesDetail {
  specCode: number
  speciesName: string
  preferredCommonName: string | null  // xem Language param bên dưới
  genusName: string | null
  familyName: string | null
  waterType: string             // "Freshwater" | "Saltwater" | "Brackish" | "Both"
  length: number | null         // cm
  weight: number | null         // kg
  dangerous: string | null
  demersPelag: string | null    // "demersal" | "pelagic" | ...
  lifeCycle: string | null
  remark: string | null

  preferredImageUrl: string | null   // presigned MinIO URL, http://
  maleImageUrl: string | null
  femaleImageUrl: string | null

  ecology: {
    feedingType: string | null
    dietTroph: number | null
    habitatZones: string[]    // e.g. ["Lakes", "Stream", "Mangroves", "Neritic"]
  } | null

  conservation: {
    iucnCode: string | null       // e.g. "LC", "VU", "EN", "CR"
    iucnAssessment: string | null
    iucnDateAssessed: string | null // ISO date string
    citesCode: string | null
  } | null

  environment: {
    tempMin: number | null    // °C
    tempMax: number | null
    phMin: number | null
    phMax: number | null
  } | null
}
```

**Language param:**
- Truyền `?language=vi` hoặc `?language=en`
- BE tự normalize: `vi`/`vn` → `Vietnamese`, `en`/`eng` → `English`
- Fallback tự động về English nếu không có tên theo ngôn ngữ yêu cầu — FE không cần xử lý

### `GET /api/species/{specCode}/media`

```typescript
interface SystemImageDto {
  id: string           // GUID
  specCode: number
  name: string         // tên file gốc
  pictureType: string
  picPreferred: boolean | null
  gender: 'Unknown' | 'Male' | 'Female' | 'Juvenile'
  url: string | null   // presigned MinIO URL, http://
}
```

### `GET /api/species/{specCode}/occurrences?limit=500`

```typescript
interface OccurrenceDto {
  id: number
  specCode: number
  countryCode: string | null
  locality: string | null
  latitudeDec: number | null
  longitudeDec: number | null
  province: string | null
}
```

---

## 📝 Implementation Notes

### Story 5.1 — Auth
- `src/lib/auth/pkce.ts` — PKCE helpers (verifier, challenge, state)
- `src/lib/auth/oidc.ts` — OIDC endpoints, exchangeCode, refreshAccessToken, revokeToken
- `src/store/authStore.ts` — Zustand store (access token in memory)
- `src/features/auth/LoginPage.tsx` + `CallbackPage.tsx`
- `src/hooks/useLogout.ts` — revoke tokens + redirect /connect/logout
- Backend: đã thêm `aquahome-fe` client vào OpenIddictSeeder + enable refresh token flow + `offline_access` scope

### Story 5.3 — Fish Search
- Gọi `GET /api/species/search` trực tiếp FishDex (port 8081) — tạm thời, refactor qua AquaHome sau
- `src/lib/api/fishDex.ts` — axios client + Bearer token
- `src/hooks/useDebounce.ts` — debounce 400ms
- `src/features/fish-search/FishSearchPage.tsx` — search input, grid, pagination, skeleton loading
- `SpeciesCard` — tên khoa học, common name badge, genus/family, nút disabled chờ Story 5.2
- `VITE_FISHDEX_API_URL=http://localhost:8081` cần thêm vào `.env.local`

**Search API — Updates đã deploy (Story 1.7 + 1.8):**
- `SpeciesSearchResult` giờ có `imageUrl?: string | null` — đã thêm vào type + `SpeciesCard.tsx`
- Truyền `language=vi` hoặc `language=en` vào search params để lấy `preferredCommonName` đúng ngôn ngữ
- Fallback: nếu không có tên tiếng Việt → tự động trả English — không cần xử lý ở FE
- `SearchSpeciesParams` đã có field `language?: string` — chỉ cần truyền lên

### Story 5.6 — Species Detail Page *(cần implement)*

**Files cần tạo mới:**
```
features/fish-detail/
  FishDetailPage.tsx
  components/
    ImageGallery.tsx      — carousel + compare đực/cái (dùng maleImageUrl / femaleImageUrl)
    EcologySection.tsx    — feedingType, dietTroph, habitatZones badges
    ConservationSection.tsx — IUCN badge, CITES, temp/pH range
    OccurrenceMap.tsx     — react-leaflet + OpenStreetMap tile
```

**API calls cần thêm vào `src/lib/api/fishDex.ts`:**
```typescript
export const getSpeciesDetail = (specCode: number, language?: string) =>
  fishDexClient.get<SpeciesDetail>(`/api/species/${specCode}/detail`, { params: { language } })

export const getSpeciesMedia = (specCode: number) =>
  fishDexClient.get<SystemImageDto[]>(`/api/species/${specCode}/media`)

export const getSpeciesOccurrences = (specCode: number, limit = 500) =>
  fishDexClient.get<OccurrenceDto[]>(`/api/species/${specCode}/occurrences`, { params: { limit } })
```

**Shared types cần thêm vào `packages/shared/src/types/species.ts`:**
```typescript
export interface SpeciesDetail { /* xem API Contract ở trên */ }
export interface SystemImageDto { /* xem API Contract ở trên */ }
export interface OccurrenceDto { /* xem API Contract ở trên */ }
```

**Wire SpeciesCard:** Nút "View Profile" navigate đến `/fish/${species.specCode}`.

**Map package cần install:**
```bash
npm install react-leaflet leaflet
npm install -D @types/leaflet
```

---

## 🎨 Design Decisions

| Concern | Quyết định | Ghi chú |
|---------|-----------|---------|
| Component library | shadcn/ui + Tailwind CSS | Copy-paste components, không vendor lock-in |
| Layout | App Shell — sidebar cố định | 4 section ngang nhau |
| Map | **react-leaflet + OpenStreetMap** | Free, không API key, hỗ trợ lat/lng pin |
| Mobile | Giữ logic ở `lib/store/hooks` | Khi port sang React Native: giữ nguyên logic, viết lại UI |

Design reference: [kaiadmin-lite](https://themewagon.github.io/kaiadmin-lite/) — layout, màu sắc, card style làm inspiration.
