# Plan FrontEnd — web + app mobile (Expo)

> Repo: monorepo `TheEastPham/FishDex` → `FrontEnd/` (npm workspaces):
> - `apps/aquahome-web` — web (React 19 + Vite), mobile-first 390px
> - `apps/mobile` — **app iOS + Android bằng React Native + Expo** (ADR 0006, chưa tạo)
> - `packages/shared` — dùng chung cho cả hai: API client, làm mới token, PKCE, kiểu dữ liệu, i18n 4 ngôn ngữ
>
> Đọc kèm [00-plan-tong.md](00-plan-tong.md) (quyết định, hợp đồng). Mỗi mục có mã sẽ thành 1 ticket.

## Hiện trạng FE (đã kiểm trong code `release/v1.2`)

- Web: React 19, react-router 7 (route tải lười), Vite 6, Tailwind, Zustand, axios, i18next (en, vi, de, zh — đủ cả 4, có `npm run i18n:check -w packages/shared`).
- Test: vitest ở gốc `FrontEnd/` (`npm test`), 5 file test (API client, PKCE, cache, media access, 1 component). `FrontEnd/CLAUDE.md`: logic trong `packages/shared` **phải có test** vì app mobile dùng lại.
- Gọi API bằng hàm axios viết tay trong `packages/shared/src/lib/api/*`; DTO viết tay trong `packages/shared/src/types/*` → **hợp đồng phải ghi đủ field**.
- Đăng nhập PKCE viết tay; access token trong bộ nhớ, refresh token trong `sessionStorage` (tài liệu FE ghi rõ là tạm cho dev).
- **Phần chỉ chạy được trên web đang nằm trong shared**: `sessionStorage`, `window`, `import.meta.env`, `cn()` (Tailwind) — ở `lib/api/client.ts`, `lib/api/fishDex.ts`, `lib/api/market.ts`, `lib/auth/oidc.ts`, `store/authStore.ts`, `lib/mediaAccess.ts`, `lib/tileConfig.ts`, `hooks/useLogout.ts`, `hooks/usePushNotification.ts`.
- Route chờ sẵn trên web: `/ai-chat`, `/image-search`, `/parameters` (placeholder).
- **Chưa có**: SSE/stream, thư viện biểu đồ, 3D, thư mục `apps/mobile`, kiểm tra tự động trên PR (F1 ở plan BackEnd làm luôn job FE).
- Web deploy: Cloudflare Pages tự build khi push.

## Nguyên tắc riêng cho FrontEnd

- **Mobile-first, iPhone 12 (390px)** cho web (`FrontEnd/CLAUDE.md`): không cuộn ngang, nút ≥ 44×44, input font ≥ 16px.
- Mọi chuỗi qua i18n, đủ **4 ngôn ngữ**; `i18n:check` phải xanh.
- **Logic dùng chung viết một lần trong `packages/shared`** (gọi API, parser SSE, kiểu dữ liệu, xử lý lỗi), có test. Web và app chỉ khác phần giao diện.
- Web: feature mới ở `apps/aquahome-web/src/features/<ten>/`; API ở `packages/shared/src/lib/api/<khu>.ts`; DTO ở `types/<khu>.ts`; export qua `packages/shared/src/index.ts`.
- Làm trên **mock theo hợp đồng** trước, không chờ BE xong.
- Lỗi từ BE dạng `{error: '<mã>', message?, ...}`: 503 → "thử lại sau" + nút thử lại; 429 `quota_exceeded` → báo hết lượt + giờ reset.
- Commit FE tách khỏi commit BE.

---

## F — Nền móng

Kiểm tra PR cho FE (vitest + `i18n:check` + build web) nằm trong **F1** của plan BackEnd (chung một workflow).

---

## R1 — Chat AI

### FE-1. Client stream có xác thực + bộ đọc SSE (shared)
**Vì sao:** axios không đọc stream được trên trình duyệt; cơ chế tự refresh khi gặp 401 trong `lib/api/client.ts` chỉ áp dụng cho axios; timeout 10 giây quá ngắn cho AI. App mobile sẽ dùng lại phần này, nên viết sao cho không phụ thuộc DOM.
**Làm:**
- `lib/sse.ts`: tách sự kiện SSE, chịu được dữ liệu bị cắt giữa chừng, nhiều dòng `data:`.
- `lib/api/stream.ts`: gọi bằng `fetch` + đọc stream, gắn Bearer; 401 → refresh 1 lần (dùng chung cơ chế chống gọi trùng với `client.ts`) rồi gọi lại; lỗi trước khi stream → trả lỗi có mã HTTP và `error`.
- `lib/api/ai.ts` (`askAi`, `getAiQuota`) + `types/ai.ts` theo C-R1, gồm cả 3 kiểu kết quả `answer` / `clarify` / `refuse`.
- Chế độ giả lập `VITE_AI_MOCK=true`.
**Xong khi:** vitest cho bộ đọc SSE (dữ liệu bị cắt, nhiều dòng, sự kiện lạ) và cho client (401 → refresh → thành công; 429/503 → lỗi đúng mã).
**Phụ thuộc:** F1, C-R1.

### FE-2. Trang Chat AI (web)
**Làm:**
- Thay placeholder `/ai-chat`: danh sách tin nhắn, ô nhập dính đáy (không bị bàn phím che), chữ hiện dần, chip trích dẫn (tên Việt / Anh / khoa học) bấm sang trang loài.
- **Hỏi lại khi mơ hồ**: kết quả `clarify` hiện danh sách loài ứng viên để chọn, rồi gửi lại với `selectedSpecCode`.
- `refuse`: hiện câu từ chối, không coi là lỗi.
- Chọn bể (không bắt buộc) bằng `useMyAquariums` có sẵn. Hiện lượt hỏi còn lại; hết lượt → báo + giờ reset.
- Lỗi 503 → nút thử lại; lỗi giữa chừng → giữ phần đã nhận + báo lỗi; rời trang → huỷ request.
- Dòng nhắc: câu trả lời chỉ mang tính tham khảo.
**Xong khi:** 390px đạt các tiêu chí mobile; chạy được với mock; build + test xanh.
**Phụ thuộc:** FE-1. Chế độ stream hay JSON tuỳ kết quả thử nghiệm SP-1.

---

## R2 — Số đo nước

### FE-3. Trang `/parameters` (web)
- Chọn bể; form nhập NH₃ / NO₂ / NO₃ / pH / nhiệt độ (bàn phím số thập phân, font ≥ 16px); lịch sử phân trang; biểu đồ xu hướng; khối cảnh báo kèm **nguồn ngưỡng** và câu "tham khảo, không thay thế xét nghiệm".
- Thêm **một** thư viện biểu đồ nhẹ; nếu muốn dùng chung cho app sau này thì ghi rõ trong PR (thư viện web thường không chạy trên React Native).
- Liên kết sang nhắc thay nước có sẵn (`features/tanks/components/RemindersSection.tsx`).
**Phụ thuộc:** C-R2.

---

## R3 — Mô phỏng thả cá

### FE-4. Báo cáo tương thích khi thêm cá (web)
Trong luồng thêm cá, gọi API mô phỏng trước khi xác nhận; hiện từng mục (nhiệt độ / pH / dH, kích thước so với bể, kiểu ăn; hung dữ và sống bầy nếu có dữ liệu) với điểm, giải thích, độ chắc chắn, hoặc **"chưa đủ dữ liệu"** — phần lớn loài sẽ rơi vào trường hợp này, nên giao diện phải hiển thị nó rõ ràng, không như lỗi. **Không chặn** thêm cá.
**Phụ thuộc:** BE-16.

---

## R4 — Tìm ảnh

### FE-5. Trang `/image-search` (web)
Thay placeholder; chụp hoặc chọn ảnh (`accept="image/*" capture="environment"`), nén bằng `packages/shared/src/lib/image.ts` có sẵn, hoặc tìm bằng chữ; lưới kết quả bấm sang trang loài; lượt còn lại, lỗi 429/503. Làm với mock trước.
**Phụ thuộc:** C-R4.

---

## Web — sửa lỗi đang có

### FE-7. Cache service worker
`apps/aquahome-web/nginx.conf` cache mọi file `.js` vĩnh viễn (`immutable`) → `push-sw.js` cũng bị cache, người dùng kẹt bản cũ. Thêm rule `no-cache` cho file service worker và `manifest.json`; thêm `public/_headers` cho Cloudflare Pages với cùng chính sách. Kiểm header bằng `curl -I` trên bản build nginx local.

---

## R5 — App mobile (Expo)

Thứ tự theo ADR 0006 và `ai-release-plan.md`: **5 việc chuẩn bị** trước → khung app + đăng nhập → các luồng chính của web → chat AI / tìm ảnh khi R1 / R4 xong. Phần chuẩn bị phía BE (validate audience, discovery OIDC, client OAuth, kênh push) ở plan BackEnd: BE-19 → BE-22.

### MB-1. Tách phần chỉ web khỏi `@fishlover/shared`
**Vì sao:** app React Native không có `sessionStorage`, `window`, `import.meta.env`, Tailwind.
**Làm:**
- Đưa phần phụ thuộc nền tảng ra sau các "adapter" nhỏ: lưu token (web: `sessionStorage`; mobile: SecureStore), cấu hình môi trường (base URL, client id), điều hướng / mở trình duyệt khi đăng nhập.
- Chuyển `cn()` và những gì chỉ web dùng về `apps/aquahome-web`.
- Web phải chạy y như cũ; các test hiện có vẫn xanh, thêm test cho adapter.
**Xong khi:** `packages/shared` không còn tham chiếu trực tiếp tới `window` / `sessionStorage` / `import.meta.env`; build web + test xanh.
**Phụ thuộc:** F1.

### MB-2. Khung app Expo
`apps/mobile` bằng Expo (TypeScript), nối vào npm workspaces, dùng `@fishlover/shared`, i18n 4 ngôn ngữ, cấu hình EAS (`eas.json`) cho iOS + Android, biến môi trường theo môi trường. Màn hình mẫu gọi 1 API public.
**Xong khi:** chạy được bằng Expo Go / dev build; workflow PR chạy thêm `tsc` cho app.
**Phụ thuộc:** MB-1.

### MB-3. Đăng nhập trên app
PKCE bằng client OAuth riêng (BE-21, redirect `fishlover://callback`); token trong **SecureStore**; tự làm mới token dùng lại logic shared.
**Phụ thuộc:** MB-2, BE-19, BE-20, BE-21, C-MB.

### MB-4 → MB-6. Các luồng chính (tách ticket khi tới lượt)
- MB-4: tra cứu loài + chi tiết loài.
- MB-5: bể của tôi, thêm/bớt cá, nhắc việc.
- MB-6: push trên app (sau BE-22).

### MB-7. Chat AI trên app
Giao diện chat native dùng lại FE-1 (client stream + parser). Kiểm stream SSE chạy được trên React Native; nếu không, dùng chế độ JSON của C-R1.
**Phụ thuộc:** FE-1, MB-3, BE-10.

### MB-8. Tìm ảnh trên app
Chụp ảnh bằng camera của máy, nén, gọi API tìm ảnh.
**Phụ thuộc:** MB-3, BE-18.

### MB-9. Thử trên máy thật và nộp store (chủ dự án)
Checklist iPhone 12+ (bố cục, safe-area, bàn phím, đăng nhập lại sau khi tắt app, push); build EAS cả 2 nền tảng; nộp App Store và Google Play.

### DEC-MB. Cần chủ dự án chốt
- Kênh push: Expo Push (đơn giản) hay FCM/APNs trực tiếp.
- Tài khoản Apple Developer (99 USD/năm) và Google Play (25 USD một lần).
- Giới hạn Expo Free (15 build iOS + 15 Android / tháng) có đủ không.

> Đã bỏ khỏi plan cũ: "cứng hoá PWA" và "cổng Capacitor" — ADR 0006 đã thay bằng app Expo. Web vẫn giữ Web Push và manifest hiện có.

---

## R6 — Bể 3D (chưa tách việc)

Hiển thị 1 bể: hộp bể + cá theo kích thước thật, nhóm đàn; chạy mượt trên iPhone 12; bản đơn giản trước. Chờ R3 xong (cần trait kích thước) và chủ dự án mở cổng. Khi mở phải chọn làm trên web, trên app hay cả hai (thư viện 3D khác nhau).

---

## Thứ tự gợi ý cho FrontEnd

1. FE-7 và MB-1 (làm ngay, không phụ thuộc AI)
2. FE-1 ngay khi C-R1 chốt → FE-2
3. MB-2 → MB-3 khi BE-19 / BE-20 / BE-21 xong → MB-4, MB-5
4. FE-3 khi C-R2 chốt
5. FE-5 khi C-R4 chốt; MB-7, MB-8 sau khi bản web chạy ổn
6. FE-4 ở R3
