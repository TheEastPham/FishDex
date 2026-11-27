# Plan tổng — AI v2.0: BackEnd, AI, FrontEnd (web + mobile) làm việc thế nào

> Bản nháp để chủ dự án đọc và sửa. Sau khi chốt, mỗi mục có mã (BE-1, AI-2, FE-3, MB-1…) sẽ thành 1 ticket GitHub.
> Plan chi tiết: [01-plan-backend.md](01-plan-backend.md) · [02-plan-ai.md](02-plan-ai.md) · [03-plan-frontend.md](03-plan-frontend.md).
> Dựa trên `release/v1.2` (đang chạy PROD) ngày 27/11/2026: [`../ai-release-plan.md`](../ai-release-plan.md), [`../r0-data-analysis.md`](../r0-data-analysis.md), ADR 0001–0006 (`BackEndProject/docs/adr/`), C4 (`docs/architecture/c4/`).
> File này **không ghi trạng thái**. Trạng thái nằm trên GitHub Project.

---

## 1. Vì sao cần plan này

- `ai-release-plan.md` nói **làm gì, theo thứ tự nào, xong khi nào**. Nó chưa nói **việc nào nằm ở repo nào**, các bên **nói chuyện với nhau qua hợp đồng gì**, và việc nào giao được cho agent.
- Một số tài liệu vẫn lệch nhau. C4 vẽ embedding service gọi Groq và Ollama chạy trên VM3. ADR 0006 và CLAUDE.md ghi service gọi VM3 "qua gateway". Release plan lại ghi VM3 "không expose public". Plan này chốt một phương án cho từng điểm (mục 3).
- Muốn giao việc cho Claude cloud hoặc GitHub agent thì mỗi việc phải tự đứng được: phạm vi rõ, hợp đồng rõ, có cách kiểm chứng tự động.

## 2. Hiện trạng (đã kiểm trong code `release/v1.2`)

| Mảng | Đã có | Chưa có / cần xử lý |
|---|---|---|
| AI / vector | Extension `vector` được bật bằng script khởi tạo DB. Chunker `Pipeline/Preprocessor/preprocess.py` (bản v1.0: 1 chunk phẳng/loài, output bị gitignore) | Package pgvector, entity, `ILlmProvider`, client VM3. VM3 chỉ có thư mục rỗng. **Bảng `SpeciesChunks` mồ côi** trong DB local (migration cũ còn trong lịch sử, không còn trong code) — phải kiểm cả PROD |
| Dữ liệu FishBase | 214 file parquet v24.07 ở laptop chủ dự án (`D:\Workspace\FishLover\data\fishbase-v24.07`), ngoài mọi repo | Phạm vi làm trước: 3.751 loài cá cảnh. Tên Việt chỉ có ở 6,6% loài; đủ nhiệt độ + pH ~26%; không có ngưỡng nitrit/nitrat, tính hung dữ, thể tích bể tối thiểu |
| Quota / role | `RoleQuota` có `AiQaPerDay`, `ImageSearchPerDay`; `QuotaService.ConsumeDailyAsync` (chưa ai gọi); filter 429 `quota_exceeded` | Endpoint "quota còn lại" cho người dùng |
| Gọi giữa service | Internal key `X-Internal-Api-Key` (mẫu `MarketIngestController`); `FishDexClient` phân biệt "lỗi" với "rỗng" → 503 | — |
| Nhắc việc / push | Web Push (VAPID) đầy đủ, `IWebPushNotifier`, worker nhắc thay nước | Push cho app mobile, số đo nước |
| FE web | React 19 + Vite, route chia nhỏ (lazy), vitest (5 file test), `i18n:check`, 4 ngôn ngữ đủ; route chờ sẵn `/ai-chat`, `/image-search`, `/parameters` | SSE/stream, biểu đồ |
| Mobile | ADR 0006: app Expo ở `FrontEnd/apps/mobile` | Chưa có thư mục app. 5 việc chuẩn bị chưa làm (mục 6, R5) |
| CI | Azure DevOps: build image arm64 → GHCR khi push (`pr: none`, không chạy test). FE do Cloudflare Pages tự build | **Không có kiểm tra tự động nào trên PR** |
| Tài liệu | CLAUDE.md BE ghi rõ phần AI là "🚧 CHƯA XÂY"; tài liệu hệ thống gom về `docs/` | C4 và CLAUDE.md còn câu "qua gateway"; C4 còn vẽ Groq/Ollama nằm ở VM3 |

**Ba lỗi phát hiện khi khảo sát, vẫn còn trong v1.2** (đưa vào nền móng):
- Token đăng nhập PKCE mang claim `role`, nhưng `CurrentUserSession.Roles` chỉ đọc `ClaimTypes.Role` → quota luôn tính theo Member, kể cả admin.
- Route bắt-tất-cả ở gateway làm các endpoint "nội bộ" (`market/ingest`, `push/send`) vẫn gọi được từ internet, chỉ còn key bảo vệ.
- nginx cache `push-sw.js` 1 năm → người dùng web có thể kẹt service worker cũ.

## 3. Quyết định

**Đã có trong v1.2:**
- ADR 0006: app mobile **React Native + Expo** ở `FrontEnd/apps/mobile`, dùng chung `@fishlover/shared`. Dịch vụ ML Python ở repo riêng **`FishLover-AI`** (private). Phần tích hợp AI trong .NET (`ILlmProvider`, RAG, entity vector, quota) **ở lại monorepo**.
- Q5 (24/11): câu hỏi được **quy về tiếng Anh** trước khi embed bằng MiniLM. Chunk giữ tiếng Anh, tên Việt đi qua bảng `SpeciesAlias`. `QueryNormalizer` chạy bằng **luật trước**, LLM dịch chỉ là fallback.

**Chủ dự án chốt ngày 27/11:**
1. Repo `FishLover-AI` chứa toàn bộ code Python AI: service VM3, job offline, bộ đánh giá.
2. Vector được ghi vào database FishDex **qua endpoint nội bộ của FishDex**. Repo AI không giữ mật khẩu database.
3. LLM dự phòng: **Gemini API**. Ollama chỉ dùng khi dev trên laptop, không chạy trên VM3.
4. Theo dõi tiến độ trên **GitHub Project chung** cho 2 repo. File md không ghi trạng thái.
5. **VM3 chỉ đi bằng IP private**: FishDex/AquaHome gọi thẳng VM3 kèm internal key. **Không** thêm route `/embeddings`, `/image-search` vào gateway.
6. **GitHub Actions chỉ dùng để kiểm tra PR** (build + test). Azure DevOps giữ vai trò build image / deploy như cũ.
7. **`QueryNormalizer` và `SpeciesAlias` đặt ở FishDex**, vì FishDex sở hữu tên loài, tên cộng đồng và chunk.

**Đề xuất mặc định** (sẽ ghi thành ADR 0007–0010, chủ dự án duyệt):

| ADR | Quyết định | Lý do |
|---|---|---|
| 0007 | VM3 chỉ trong mạng private, gọi bằng IP nội bộ + internal key. VM3 down → 503, không fallback | Sửa câu "qua gateway" trong ADR 0006: gateway là cổng public `api.fishlover.org` |
| 0008 | **FishDex sở hữu tri thức**: chunk, vector, alias, chuẩn hoá câu hỏi, trait. Dữ liệu vào qua `api/internal/**`. AquaHome không đọc database FishDex | Trả lời câu hỏi còn mở trong C4: "Ai ghi embedding vào database FishDex?" |
| 0009 | **AquaHome gọi LLM** qua `ILlmProvider`: Groq → Gemini; cả hai lỗi → 503, không tự thử lại. AquaHome giữ mọi quota AI (chat, tìm ảnh, cả lượt LLM dịch fallback). Chat ở `/aquahome/v1/ai/...` | C4 đang vẽ embedding service gọi Groq — trái ADR 0006 |
| 0010 | Kiểm tra PR bằng GitHub Actions (chỉ test), deploy vẫn ở Azure DevOps | Đảo một phần việc v1.2 vừa bỏ template GitHub Actions, nên phải ghi lý do |

Quy ước kỹ thuật (ghi trong CLAUDE.md, không cần ADR): tìm vector bằng cosine `<=>` với index HNSW `vector_cosine_ops`; vector 384-d (chunk) và 512-d (ảnh) không trộn trong một truy vấn.

## 4. Các project và ranh giới

| Project | Ở đâu | Làm gì | Không làm gì |
|---|---|---|---|
| **BackEnd** | Monorepo → `BackEndProject/` + `Pipeline/` (VM1, VM2 và **hạ tầng VM3**: compose, README) | Schema + migration mọi database; API public `/v1` (dùng chung cho web và app, giữ tương thích ngược); endpoint nội bộ; alias + chuẩn hoá câu hỏi + tìm tri thức (FishDex); quota, gọi LLM, luật cảnh báo, analyzer (AquaHome); OAuth/push cho mobile (UserManagement) | Không chạy model ML |
| **AI** | Repo private `TheEastPham/FishLover-AI` | Service embedding (:8000) và CLIP (:8001); job offline: dựng chunk theo chủ đề từ parquet, nạp chunk, backfill ~70k ảnh, trích trait bù; bộ đánh giá theo tầng; sau v2.0: thu gọn model cho điện thoại | Không ghi database, không xác thực người dùng, không giữ quota, không gọi LLM ở đường online |
| **FrontEnd web** | Monorepo → `FrontEnd/apps/aquahome-web` | Giao diện web mobile-first (390px) | Không gọi VM3 hay LLM |
| **FrontEnd mobile** | Monorepo → `FrontEnd/apps/mobile` (Expo) | App iOS + Android, dùng chung `@fishlover/shared`, build bằng EAS | Như web |

Image của repo AI được CI của chính repo đó build (`linux/arm64`) và đẩy lên GHCR. VM3 kéo image về chạy bằng compose ở `Pipeline/OracleVM/VM3` — giống cách VM1 đang chạy các service .NET.

Luồng chạy thật:
```
Web / App mobile
  ├─ /aquahome/v1/ai/chat ─► AquaHome: kiểm quota
  │     ├─► FishDex /api/internal/knowledge/search (câu hỏi gốc tiếng Việt)
  │     │     QueryNormalizer: alias → SpecCode, chủ đề → chunkType, dựng câu tiếng Anh
  │     │     ├─ khớp chắc → lọc SpecCode + chunkType → VM3 /embed → pgvector (<=>) → chunk
  │     │     ├─ nhiều loài → trả danh sách ứng viên → AquaHome hỏi lại người dùng
  │     │     └─ không hiểu → AquaHome nhờ LLM dịch (có cache, tính RPM) → gọi lại search bằng câu tiếng Anh
  │     └─► Groq (lỗi → Gemini): sinh câu trả lời tiếng Việt từ chunk tiếng Anh + trích dẫn
  ├─ /aquahome/v1/ai/image-search ─► AquaHome: quota → VM3 /embed/image → FishDex tìm ảnh gần nhất
  └─ /aquahome/v1/aquariums/{id}/readings ─► AquaHome: lưu số đo, luật cảnh báo (không AI)

Job offline (repo AI, chạy trên laptop / container một lần trong mạng private)
  ──PUT /api/internal/... (internal key)──► FishDex: chunk + vector, vector ảnh, trait trích xuất
```

## 5. Hợp đồng giữa các project

Hợp đồng giúp các project làm **song song**: chốt hợp đồng trước, mỗi bên làm theo, bên chờ dùng bản giả lập (mock). API public dùng chung cho web và app, nên **mọi thay đổi phải tương thích ngược trong `/v1`**.

| Mã | Hợp đồng | Bên cung cấp → bên dùng | Nơi lưu sau khi chốt |
|---|---|---|---|
| C-R0 | Embedding service; nạp chunk; chuẩn hoá + tìm tri thức | AI → BE; BE → AI (job nạp); FishDex → AquaHome | `FishLover-AI/openapi/embedding.v1.yaml`, `BackEndProject/docs/contracts/fishdex-knowledge.v1.md` |
| C-R1 | Chat AI + xem quota | BE → web, mobile | `BackEndProject/docs/contracts/aquahome-ai-chat.v1.md` + `FrontEnd/packages/shared/src/types/ai.ts` |
| C-R2 | Số đo nước + cảnh báo | BE → web, mobile | `BackEndProject/docs/contracts/aquahome-water.v1.md` |
| C-R3 | Trait loài + mô phỏng thả cá | BE ↔ AI, BE → web, mobile | sau DEC-R3 |
| C-R4 | CLIP service + tìm ảnh | AI → BE, BE → web, mobile | `FishLover-AI/openapi/image-search.v1.yaml` + contract AquaHome |
| C-MB | OAuth client mobile, push mobile | BE → mobile | sau DEC-MB |

### C-R0 — nháp
**Embedding service (VM3 :8000).** Bắt buộc header `X-Internal-Api-Key`, trừ `/health` và `/metrics`.
- `GET /health` → `{status, model, dims: 384}`
- `POST /embed` `{text (1–2000 ký tự), normalize = true}` → `{embedding: 384 số, model, latencyMs}`
- `POST /embed/batch` `{texts (1–64 câu), normalize}` → `{embeddings, model, latencyMs}`
- Lỗi: 401 sai key · 422 dữ liệu sai · 503 model chưa nạp xong.
- Cache 1 giờ theo MD5 của text (ràng buộc CLAUDE.md) — đặt ở phía gọi (FishDex), không ở service.

**FishDex nội bộ** (chỉ gọi được trong mạng private, bị chặn ở gateway):
- `PUT /api/internal/knowledge/chunks` — gửi theo loài, mỗi chunk có `chunkIndex`, `chunkType` (overview / water / feeding / breeding / care…), `text` (tiếng Anh), `contentHash`, `embedding` 384. Sau khi gọi, tập chunk của loài đó **đúng bằng** tập vừa gửi. Tối đa 500 chunk/lần. 400 nếu sai số chiều hoặc mã loài không tồn tại.
- `GET /api/internal/knowledge/chunks/hashes?specCodes=` — để job bỏ qua chunk không đổi.
- `POST /api/internal/knowledge/search`:
  - Vào: `{question}` (câu gốc tiếng Việt) **hoặc** `{englishQuery, specCodes?, chunkTypes?}` (khi AquaHome đã dịch bằng LLM), cộng `topK ≤ 20`.
  - Ra: `{resolution: {status: resolved | ambiguous | unknown, species: [{specCode, scientificName, englishName, vietnameseName, confidence}], chunkTypes, englishQuery}, results: [{specCode, chunkIndex, chunkType, text, score}]}`.
  - `ambiguous` → không tìm, trả ứng viên để hỏi lại. `unknown` → AquaHome quyết định có dịch bằng LLM không.
  - Không có kết quả → danh sách rỗng (200), **khác** với lỗi. VM3 lỗi → 503 `embedding_unavailable`.

### C-R1 — nháp
`POST /aquahome/v1/ai/chat` (đã đăng nhập) với `{question (1–500), aquariumId?, selectedSpecCode?}`
- Chế độ stream (SSE): sự kiện `meta` (quota) → `token` (nhiều lần) → `citations` (loài + chunk được dùng, hiện tên Việt/Anh/khoa học) → `done` (provider, có từ cache không, có phải dịch không). Lỗi giữa chừng → sự kiện `error`.
- Chế độ JSON: `{type: answer | clarify | refuse, answer?, candidates?, citations, quota, provider, cached}`.
- `clarify`: câu hỏi khớp nhiều loài → trả danh sách ứng viên; FE cho chọn rồi gửi lại với `selectedSpecCode`.
- `refuse`: thiếu dữ liệu hoặc ngoài phạm vi → câu từ chối chuẩn, không bịa.
- Lỗi trước khi trả lời: 400 · 401 · 404 bể không phải của mình · 429 `quota_exceeded` · 503 `ai_unavailable`.

`GET /aquahome/v1/ai/quota` → lượt đã dùng / giới hạn / thời gian reset cho chat và tìm ảnh (`-1` là không giới hạn).

### C-R2 — nháp
- Thêm, xem (phân trang), xoá số đo: `/aquahome/v1/aquariums/{id}/readings` với NH₃, NO₂, NO₃, pH, nhiệt độ (mỗi chỉ số có thể trống, ít nhất 1).
- `GET /aquahome/v1/aquariums/{id}/water-status` → số đo mới nhất + cảnh báo (chỉ số, mức info/warning/danger, giá trị, ngưỡng, xu hướng, **nguồn trích dẫn**) + câu "tham khảo, không thay thế xét nghiệm".

### C-R4 — nháp
- CLIP service: `/embed/text`, `/embed/image` (ảnh ≤ 5MB, jpg/png/webp) → vector 512.
- FishDex nội bộ: lấy danh sách ảnh cần embed, ghi vector, tìm theo vector.
- Public: `POST /aquahome/v1/ai/image-search` (ảnh hoặc chữ) → `{results: [{specCode, name, imageUrl, score}], quota}`.

## 6. Lộ trình theo release — ai làm gì

| Release | BackEnd | AI | FE web | Mobile | Chủ dự án |
|---|---|---|---|---|---|
| **F – Nền móng** | Kiểm tra PR, sửa tài liệu lệch, chặn route nội bộ, sửa lỗi role, glossary | Tạo repo, chuyển preprocess, kiểm thư viện trên arm64 | (chung workflow PR) | — | Cài `gh`, tạo repo, duyệt ADR 0007–0010 |
| **R0 – Dữ liệu + embedding + chuẩn hoá câu hỏi** | Xử lý bảng `SpeciesChunks` mồ côi, pgvector, chunk có `chunkType`, `SpeciesAlias`, `QueryNormalizer`, endpoint nạp + tìm | Chunk theo chủ đề (tiếng Anh, 3.751 loài), embedding service, job nạp, bộ đánh giá theo tầng + baseline | — | — | Chốt C-R0, duyệt seed tên chơi cá, soạn đáp án đánh giá, bật VM3 |
| **R1 – Hỏi đáp AI** | LLM provider, dịch fallback, dịch vụ chat, controller, chunk + alias cho cá lai | Đánh giá end-to-end | Client stream, trang Chat | (sau khi app có khung) | Chốt C-R1, thử SSE, nạp key, ngưỡng chất lượng, rà soát bảo mật |
| **R2 – Số đo nước** | Test AquariumService, bảng số đo, luật cảnh báo (ngưỡng ngoài FishBase, có nguồn), push | — | Trang số đo + biểu đồ | (sau) | Nguồn ngưỡng, thời gian lưu, role |
| **R3 – Mô phỏng thả cá** | Trait (gộp nhiều stock, bỏ biên >15°C, tách ngọt/mặn), analyzer, endpoint mô phỏng | Trích trait bù (có duyệt) | Báo cáo tương thích | (sau) | Schema trait, ngưỡng đủ dữ liệu, nguồn tính cách/sống bầy |
| **R4 – Tìm ảnh** (song song) | Bảng vector ảnh, tìm ảnh, BFF | CLIP service, backfill 70k ảnh | Trang tìm ảnh | (sau) | Chốt C-R4, deploy CLIP |
| **R5 – Mobile** | Validate audience, sửa discovery OIDC, client OAuth mobile, kênh push mobile | — | Tách phần chỉ web khỏi `@fishlover/shared`, sửa cache SW | Khung app Expo, đăng nhập, luồng chính, rồi chat/tìm ảnh | Chọn kênh push, tài khoản Apple/Google, duyệt store |
| **R6 – Bể 3D** | (trait kích thước từ R3) | — | Chưa tách việc | Chưa tách việc | Mở cổng sau R3 |
| **R7 – Dự đoán** | — | — | — | — | Chờ đủ dữ liệu số đo |

5 việc chuẩn bị mobile của ADR 0006 nằm ở BE và FE, **không phụ thuộc AI**, nên làm sớm được.

## 7. Những gì làm song song được ngay

```
Sau F:
  BackEnd : kiểm tra PR → xử lý SpeciesChunks mồ côi + pgvector → alias / normalizer → endpoint nạp, tìm
            song song: sửa lỗi role, chặn route nội bộ, test AquariumService (R2), validate audience + OIDC discovery (R5)
  AI      : tạo repo → kiểm thư viện arm64 → chunk theo chủ đề → embedding service ;  khung CLIP (R4) dựng sớm được
  FE web  : sửa cache SW ; tách phần chỉ web khỏi shared (R5) ; client stream ngay khi C-R1 chốt
  Mobile  : khung app Expo + đăng nhập ngay khi OAuth client mobile + OIDC discovery xong
  Chủ DA  : duyệt ADR, chốt C-R0 / C-R1, duyệt seed alias, chốt DEC-R2, chọn kênh push  ← các nút cổ chai
```

Quy tắc: một việc **chỉ bắt đầu khi hợp đồng nó cần đã chốt**. Bên làm sau dùng mock theo hợp đồng, không chờ bên kia code xong.

## 8. Việc chỉ chủ dự án làm được

| Mã | Việc | Release |
|---|---|---|
| F4 | Duyệt ADR 0007–0010 (agent soạn) | F |
| F7 | Cài `gh`, tạo repo `FishLover-AI`, Project, label, milestone; bật agent cho 2 repo | F |
| C-R0, C-R1, C-R2, C-R4, C-MB | Chốt hợp đồng | theo release |
| DEC-R0 | Duyệt danh sách seed tên chơi cá cho `SpeciesAlias`; quy tắc tên mơ hồ; có dùng tên Anh `FBname` làm alias mặc định không | R0 |
| DEC-R2 | Nguồn ngưỡng độc tố (ngoài FishBase), thời gian lưu số đo, role được dùng | R2 |
| DEC-R3 | Schema trait, ngưỡng "đủ dữ liệu", nguồn cho tính hung dữ / sống bầy (đóng góp cộng đồng hay trích xuất có duyệt) | R3 |
| DEC-MB | Kênh push mobile (Expo Push hay FCM/APNs); tài khoản Apple Developer (99 USD/năm) và Google Play (25 USD) | R5 |
| AI-3 | Soạn và duyệt đáp án ~30 câu hỏi đánh giá (cần kiến thức nuôi cá) | R0 |
| OPS-1, OPS-3 | Bật VM3, deploy CLIP, đo thật trên máy ARM | R0, R4 |
| OPS-2 | Nạp key Groq/Gemini vào PROD | R1 |
| SP-1 | Thử SSE qua Cloudflare → nginx → Ocelot | R1 |
| SEC-1 | Rà soát bảo mật R1 | R1 |
| AI-4 | Chốt ngưỡng chất lượng R1 (đúng, trích dẫn đúng, từ chối đúng, tỷ lệ phải dịch bằng LLM) | R1 |
| MB-9 | Thử trên iPhone 12 thật, nộp duyệt App Store / Google Play | R5 |

## 9. Theo dõi tiến độ (sau khi plan được duyệt)

- **1 GitHub Project** gom việc của cả 2 repo. Cột thông tin: Release (F, R0…R7), Track (BE / AI / Web / Mobile / Infra), Agent (ready / human), Status.
- Mỗi repo có milestone F, R0…R7 và label: `area:*`, `svc:*`, `type:contract|feature|bug|spike|decision`, `agent-ready`, `needs-human`.
- Mỗi release là 1 issue "epic", các việc là issue con; phụ thuộc ghi bằng quan hệ "blocked by".
- File md chỉ giữ thứ bền: ADR, hợp đồng, lý do và tiêu chí release. Bước 7 trong `ai-release-plan.md` ("cập nhật trạng thái trong tài liệu này") đổi thành "cập nhật trên GitHub Project" — việc F3.

**Một việc được gắn `agent-ready` khi**: hợp đồng đã chốt, không cần secret / VM / dữ liệu FishBase trên laptop / iPhone / tài khoản store, và kiểm chứng được bằng workflow PR. Mỗi ticket có: bối cảnh (link ADR/hợp đồng), phạm vi (file chính, mẫu code tái dùng), tiêu chí xong (đo được, có lệnh test), ngoài phạm vi, ràng buộc từ CLAUDE.md, phụ thuộc.

Lưu ý: dữ liệu FishBase **không nằm trong repo nào**. Việc cần parquet thật (dựng chunk, chạy job nạp, đo baseline) phải chạy trên laptop chủ dự án. Agent chỉ làm phần code + test với dữ liệu mẫu nhỏ.

Đề xuất thử trước với 1 việc nhỏ (F8 hoặc F5): giao cho agent, xem ticket đã đủ thông tin chưa, rồi chỉnh mẫu trước khi giao hàng loạt.

## 10. Rủi ro

| Rủi ro | Cách chặn |
|---|---|
| Alias sai/mơ hồ → trả lời sai loài một cách tự tin | Khớp nhiều loài thì hỏi lại; Confidence; bộ test riêng cho alias; chỉ lọc cứng theo SpecCode khi khớp chắc |
| LLM dịch fallback làm hết RPM hoặc vượt p95 1,5s | Luật trước; fallback có cache; đo tỷ lệ fallback trong bộ đánh giá; tính vào ngân sách 30 RPM |
| SSE bị giữ lại ở proxy | SP-1 làm sớm; hợp đồng luôn có chế độ JSON |
| VM3 (ARM, 6GB) chậm hoặc thiếu RAM | Đo trên chính VM3; cân nhắc ONNX trước khi đổi máy |
| Thư viện Python không có bản ARM | AI-0 cài thử `torch`, `sentence-transformers`, `open_clip_torch` trên arm64 ngay khi tạo repo |
| Bảng `SpeciesChunks` mồ côi va với entity mới | BE-1 kiểm local + PROD trước, xử lý bằng migration có chủ đích |
| App đã cài không cập nhật cùng lúc với BE | Giữ `/v1` tương thích ngược; đổi phá vỡ thì ra `/v2` |
| Cá lai (Community species) bị bỏ sót | BE-6 tạo chunk + alias cho cá cộng đồng; bộ đánh giá có câu về cá lai |
| Prompt injection qua mô tả loài do người dùng gửi | Ngữ cảnh luôn đặt trong khung dữ liệu; SEC-1 kiểm riêng |
| Agent làm theo tài liệu lệch | F3 sửa C4 / CLAUDE.md / ADR 0006 trước khi giao việc |

## 11. Còn mở

- Ngưỡng chất lượng R1 cho từng tầng đo.
- Model LLM dùng để dịch fallback (Groq model nhỏ hay cùng model với câu trả lời) và độ trễ thực tế.
- Quy tắc gộp nhiều `stock` của một loài khi tính trait.
- Có giữ lịch sử hội thoại phía server không (đề xuất: chưa).
- Chạy job nạp dữ liệu PROD ở đâu (đề xuất: container một lần trên VM3, trong mạng private).
