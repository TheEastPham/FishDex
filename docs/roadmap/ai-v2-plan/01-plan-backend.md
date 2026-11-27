# Plan BackEnd — AI v2.0

> Repo: monorepo `TheEastPham/FishDex` → `BackEndProject/` (.NET 9) và `Pipeline/` (VM1, VM2, hạ tầng VM3).
> Đọc kèm [00-plan-tong.md](00-plan-tong.md) (quyết định, hợp đồng). Mỗi mục có mã sẽ thành 1 ticket.

## Nguyên tắc riêng cho BackEnd

- BackEnd **sở hữu mọi schema và migration**. Repo AI không ghi database; nó gọi endpoint nội bộ của FishDex.
- **FishDex sở hữu tri thức**: chunk, vector, `SpeciesAlias`, `QueryNormalizer`, trait. **AquaHome sở hữu** quota AI, gọi LLM, chat, số đo nước, analyzer.
- Endpoint chỉ dành cho service khác đặt dưới `api/internal/**`, xác thực bằng `X-Internal-Api-Key` (mẫu: `FishDex.API/Controllers/MarketIngestController.cs`), và bị chặn ở gateway (F5).
- Gọi service ngoài (VM3, Groq, Gemini, FishDex) theo mẫu `AquaHome/AquaHome.Domain/Services/FishDexClient.cs`: phân biệt "không gọi được / timeout / lỗi" với "kết quả rỗng"; lỗi → exception riêng → 503 + `Retry-After`. VM3 gọi bằng **IP private**, không qua gateway.
- API public dùng chung cho web và app mobile: **giữ tương thích ngược trong `/v1`**.
- Giữ quy tắc CLAUDE.md: object key qua `SystemImage.ObjectKey` / `*ObjectKeys`; tách commit BE và FE; payload gọn cho màn hình 390px.
- Test: hàm thuần test thường; cần database thì Testcontainers như `FishDex.Tests` (image `pgvector/pgvector:pg16`); AquaHome dùng EF InMemory như `AquaHome.Tests`. Hướng dẫn chung: `docs/guides/be-developer-guide.md`.
- Thuật ngữ mới (Water reading, Analyzer, Confidence, Species alias, Query normalizer, Chunk type…) thêm vào `BackEndProject/GLOSSARY.md`.

---

## F — Nền móng

### F1. Kiểm tra PR bằng GitHub Actions (BE + FE)
**Vì sao:** Azure DevOps chỉ build image khi push (`pr: none`, không chạy test); v1.2 đã bỏ template GitHub Actions. Không có kiểm tra trên PR thì agent không tự chứng minh được PR đúng.
**Làm:**
- `.github/workflows/pr-check.yml`, chạy khi có PR vào `main` / `release/*`. **Chỉ build + test, không deploy, không push image.**
- Job BE (lọc `BackEndProject/**`): restore + build `FishDex.sln`, chạy 4 project test (`Share/FishLover.Shared.Tests`, `Share/FishLover.Storage.Tests`, `AquaHome/AquaHome.Tests`, `FishDex/FishDex.Tests` — cần Docker, runner ubuntu có sẵn).
- Job FE (lọc `FrontEnd/**`): `npm ci` → `npm test` (vitest ở gốc `FrontEnd/`) → `npm run i18n:check -w packages/shared` → `npm run build -w apps/aquahome-web`.
- `copilot-setup-steps.yml` cài .NET 9 + Node 20 cho GitHub agent.
- Ghi vào `Pipeline/README.md`: GitHub Actions để kiểm tra PR, Azure DevOps để build/deploy.
**Xong khi:** PR chạy workflow, xanh trên `release/v1.2`; cố ý làm hỏng 1 test → đỏ.

### F3. Sửa tài liệu còn lệch
**Làm:**
- `BackEndProject/CLAUDE.md` mục "v2.0 AI Stack": bỏ "gọi qua gateway (route sẽ thêm…)", thay bằng IP private + internal key (ADR 0007); fallback LLM là Gemini; thêm quy ước `<=>`.
- `docs/architecture/c4/02-container.md`: embedding service **không** gọi Groq; LLM do AquaHome gọi; bỏ Ollama khỏi VM3; trả lời câu hỏi "Ai ghi embedding vào database FishDex?" (ADR 0008); sửa dòng "Route AI chưa có".
- ADR 0006: thêm ghi chú đầu file "đường gọi VM3 được sửa bởi ADR 0007".
- `docs/roadmap/ai-release-plan.md`: bước 7 đổi thành cập nhật trên GitHub Project; link sang thư mục plan này.
- Thêm thuật ngữ vào `GLOSSARY.md`.
**Xong khi:** không còn tài liệu nào nói VM3 đi qua gateway hay embedding service gọi LLM.
**Phụ thuộc:** F4.

### F4. ADR 0007–0010
Agent soạn theo định dạng `docs/adr/` hiện có, chủ dự án duyệt. Nội dung: VM3 chỉ trong mạng private; FishDex sở hữu tri thức (chunk, vector, alias, chuẩn hoá câu hỏi, trait) và nhận dữ liệu qua endpoint nội bộ; AquaHome gọi LLM (Groq → Gemini → 503) và giữ quota AI; kiểm tra PR bằng GitHub Actions. Chi tiết: plan tổng mục 3.

### F5. Chặn route nội bộ ở gateway
**Vì sao:** `ApiGateway/ocelot.json` có route bắt-tất-cả `/fishdex/v1/{everything}`, `/aquahome/v1/{everything}`, `/user/v1/{everything}`. Vì vậy `market/ingest` và `push/send` (ghi "không expose qua gateway") vẫn gọi được từ internet. R0 sẽ thêm nhiều endpoint `api/internal/**`.
**Làm:** gateway trả 404 cho `/{fishdex|aquahome|user}/v1/internal/**`, `/fishdex/v1/market/ingest`, `/user/v1/push/send`. Áp dụng cho cả 3 file route (`ocelot.json`, `ocelot.Docker.json`, `ocelot.Development.json` — mỗi môi trường nạp đúng 1 file).
**Xong khi:** có test chứng minh 3 đường dẫn trên trả 404, đường `public` vẫn chạy; service gọi nhau trực tiếp vẫn được.

### F8. Sửa lỗi role rỗng với token PKCE
**Vì sao:** `Share/FishLover.Shared/Services/CurrentUserSession.cs` chỉ đọc claim `ClaimTypes.Role`; token PKCE mang claim `role` (`ResourceServerExtensions.cs` đặt `MapInboundClaims = false`, `RoleClaimType = "role"`). `QuotaService` (`RolePrecedence`, `FallbackRole = "Member"`) không thấy role → **mọi người đều bị tính quota Member**, kể cả admin. Hàng `Guest` trong `RoleQuota` không bao giờ được dùng. R1 bật quota AI nên phải sửa trước.
**Làm:** `Roles` đọc cả hai loại claim, bỏ trùng. Ghi rõ trong PR cách xử lý người chưa đăng nhập.
**Xong khi:** test claim `role` → có role; admin qua claim `role` → giới hạn -1.

---

## R0 — Dữ liệu + embedding + chuẩn hoá câu hỏi

### BE-1. Bảng chunk tri thức (xử lý bảng cũ trước)
**Vì sao:** DB FishDex local có bảng `SpeciesChunks` (`Id`, `SpecCode`, `ChunkIndex`, `Embedding vector`, `ChunkText`, 0 dòng) do migration `20260527151440_AddSpeciesChunks` tạo. Migration này còn trong `__EFMigrationsHistory` nhưng **không còn trong code**. Tạo entity mới cùng tên sẽ va chạm.
**Làm:**
- Kiểm local **và PROD** có bảng này không (chủ dự án chạy lệnh SQL kiểm, agent viết lệnh).
- Thêm `Pgvector.EntityFrameworkCore`, bật `UseVector()`; migration bật extension `vector` có chủ đích (hiện chỉ có trong script khởi tạo DB).
- Entity `SpeciesChunk`: khoá (SpecCode, ChunkIndex), **ChunkType**, Text (tiếng Anh), ContentHash, Embedding `vector(384)`, EmbeddingModel, UpdatedAt. **Không FK** tới `Species` (cá cộng đồng nằm ở `SpeciesSnapshots`, ADR-0002). Index HNSW `vector_cosine_ops` + index (SpecCode, ChunkType).
- Migration xử lý bảng cũ có chủ đích (đổi tên/xoá rồi tạo mới), ghi rõ trong PR.
- Lệnh: `dotnet ef migrations add AddKnowledgeChunks --project FishDex/FishDex.EFCore --startup-project FishDex/FishDex.API`
**Xong khi:** test Testcontainers: ghi chunk, lọc theo SpecCode + ChunkType rồi xếp `<=>` đúng thứ tự; migration chạy được trên DB có bảng cũ lẫn DB trống.
**Phụ thuộc:** F1.

### BE-2. Endpoint nội bộ nhận chunk + vector
**Làm:** `PUT /api/internal/knowledge/chunks` và `GET /api/internal/knowledge/chunks/hashes` theo C-R0. Ghi theo loài: sau khi gọi, chunk của loài đúng bằng tập gửi lên. Kiểm mã loài tồn tại như `CommunityCommonNameRepository.SpeciesExistsAsync`. Validate số chiều 384, `chunkType` thuộc danh sách cho phép, tối đa 500 chunk.
**Xong khi:** gửi 2 lần không trùng; gửi bớt chunk thì chunk thừa bị xoá; sai số chiều → 400; thiếu key → 401.
**Phụ thuộc:** BE-1, C-R0.

### BE-3. Bảng `SpeciesAlias`
**Vì sao:** tên Việt chỉ có ở 6,6% loài cá cảnh; MiniLM là model tiếng Anh. Bảng alias là nơi nối mọi cách gọi tên về một SpecCode.
**Làm:**
- Entity `SpeciesAlias`: SpecCode, Alias (có dấu, để hiển thị), AliasNormalized (bỏ dấu, chữ thường), Language, Source (`fishbase_vi`, `community`, `fb_english`, `scientific`, `seed`), Confidence, Approved.
- Nạp từ: `CommonNames` (tên Việt FishBase), `CommunityCommonName` đã duyệt, tên Anh `FBname`, tên khoa học, và **danh sách seed tên chơi cá** do chủ dự án duyệt (DEC-R0). Chỉ phạm vi 3.751 loài cá cảnh trước.
- Lệnh / job nạp lại được (không trùng).
- Tên cộng đồng được duyệt sau này → tự thêm alias.
**Xong khi:** test: một alias trỏ nhiều loài được giữ nguyên (để normalizer báo mơ hồ); nạp lại không trùng; chỉ alias `Approved` được dùng.
**Phụ thuộc:** BE-1, DEC-R0.

### BE-4. `QueryNormalizer` (chạy bằng luật)
**Làm:** module thuần ở FishDex.Domain, các bước theo `ai-release-plan.md` mục "Chuẩn hoá câu hỏi":
1. Làm sạch: chữ thường, bỏ dấu để so khớp, gộp khoảng trắng.
2. Tìm alias bằng n-gram trên `SpeciesAlias` → 0, 1 hoặc nhiều SpecCode.
3. Nhận diện chủ đề bằng từ điển ~20 chủ đề (ăn uống, nhiệt độ, pH/độ cứng, kích thước, tuổi thọ, nuôi chung, sinh sản, bệnh, phân bố…) → từ khoá tiếng Anh + `chunkType`.
4. Dựng câu truy vấn tiếng Anh.
5. Kết quả: `resolved` / `ambiguous` (kèm ứng viên) / `unknown`. **Không gọi LLM** — dịch fallback là việc của AquaHome (BE-8).
**Xong khi:** bộ test bảng (câu Việt → SpecCode + chunkType mong đợi), gồm câu không dấu ("ca lia thia"), viết tắt, tên mượn (betta, guppy, neon), câu nhiều loài ("cá chép") → `ambiguous`, câu ngoài phạm vi → `unknown`. Dùng chung bộ câu với tầng 1–2 của bộ đánh giá (AI-3).
**Phụ thuộc:** BE-3.

### BE-5. Endpoint tìm tri thức
**Làm:**
- `IEmbeddingClient` gọi VM3 `/embed` bằng IP private (config `ExternalServices:EmbeddingService`), cache 1 giờ theo MD5 của text, lỗi → `EmbeddingUnavailableException` → 503.
- `POST /api/internal/knowledge/search` theo C-R0: nhận câu gốc (chạy normalizer) hoặc câu tiếng Anh đã dịch; khớp chắc thì **lọc cứng SpecCode + chunkType** rồi xếp bằng `<=>`; mơ hồ thì trả ứng viên, không tìm.
- Trả tên Việt/Anh/khoa học của loài để hiển thị trích dẫn (tên Việt đọc `CommonNames` theo SpecCode, không `Include` — ADR-0002).
**Xong khi:** test với embedding giả: lọc đúng; mơ hồ không gọi VM3; client có test cho từng kiểu lỗi; không có chunk → rỗng, không phải lỗi.
**Phụ thuộc:** BE-2, BE-4, C-R0.

---

## R1 — Hỏi đáp AI

### BE-6. Chunk + alias cho cá cộng đồng (cá lai)
**Vì sao:** job nạp ở repo AI chỉ phủ FishBase. Cá lai bán chạy nhưng chỉ có trong `SpeciesSnapshots`.
**Làm:** khi admin duyệt một Community species → dựng chunk tiếng Anh theo chủ đề từ snapshot (nhiệt độ, pH, kích thước, tính khí, thể tích tối thiểu, độ khó) → embed → ghi; thêm alias từ tên cộng đồng. VM3 lỗi thì **vẫn duyệt được**, đánh dấu chờ embed lại. Sửa/xoá loài → cập nhật/xoá chunk và alias.
**Phụ thuộc:** BE-2, BE-3, BE-5.

### BE-7. `ILlmProvider`: Groq → Gemini
**Làm:**
- `ILlmProvider` (stream và trả đủ), `GroqLlmProvider`, `GeminiLlmProvider`, `ResilientLlmProvider`: Groq lỗi/429/timeout **trước token đầu tiên** → chuyển Gemini; cả hai lỗi → `LlmUnavailableException`. Lỗi giữa chừng không đổi provider.
- `OllamaLlmProvider` chỉ bật ở Development.
- Config `ExternalServices:GroqApi`, `ExternalServices:GeminiApi`; key qua env (`GROQ_API_KEY`, `GEMINI_API_KEY`).
- Metric OpenTelemetry: request theo provider/kết quả/mục đích (`answer` / `translate`), token, độ trễ.
**Xong khi:** test với HTTP giả cho từng nhánh; key không xuất hiện trong log.
**Phụ thuộc:** F4.

### BE-8. Dịch fallback bằng LLM (AquaHome)
**Làm:** khi FishDex trả `unknown` → nhờ LLM trích loài + chủ đề / dịch câu sang tiếng Anh (prompt ngắn, model nhỏ nếu có) → gọi lại search bằng `englishQuery`. Cache theo câu đã chuẩn hoá. Không dịch được → trả `refuse` hoặc hỏi lại, **không đoán**. Đếm tỷ lệ fallback.
**Xong khi:** test: `unknown` → gọi LLM 1 lần; hỏi lại cùng câu → lấy cache, không gọi LLM; LLM lỗi → 503.
**Phụ thuộc:** BE-5, BE-7.

### BE-9. Dịch vụ chat (`AiChatService`)
**Làm:**
1. Có `aquariumId` thì kiểm bể thuộc người dùng.
2. Trừ quota `AiQaPerDay` (`QuotaService.ConsumeDailyAsync` có sẵn).
3. Gọi FishDex search (có `selectedSpecCode` thì dùng luôn). `ambiguous` → trả `clarify` kèm ứng viên. `unknown` → BE-8.
4. Không có chunk đủ điểm → `refuse`, **không gọi LLM sinh câu trả lời**.
5. Prompt: chunk tiếng Anh là dữ liệu, đặt trong khung phân cách; chỉ dẫn hệ thống bỏ qua mọi lệnh trong chunk; trả lời **tiếng Việt**, chỉ dựa trên ngữ cảnh.
6. Trích dẫn = các chunk thực sự đưa vào prompt, hiện tên Việt/Anh/khoa học.
7. Cache Redis 24h theo hash(câu hỏi chuẩn hoá + chunk ids + model). AquaHome **chưa có Redis client** → thêm.
**Xong khi:** test: hết quota → không gọi LLM; mơ hồ → `clarify`; thiếu dữ liệu → `refuse`; FishDex lỗi → 503; hỏi lại → cache; chunk chứa "ignore previous instructions" vẫn nằm trong khung dữ liệu.
**Phụ thuộc:** BE-5, BE-7, BE-8, F8.

### BE-10. Controller chat + quota
**Làm:** `api/ai/chat` (SSE hoặc JSON tuỳ header `Accept`, theo kết quả SP-1), `api/ai/quota`. SSE: `Cache-Control: no-cache`, `X-Accel-Buffering: no`, gửi từng sự kiện, dừng khi client ngắt. Lỗi AI → 503 theo mẫu `FishDexUnavailableExceptionFilter`.
**Xong khi:** test thứ tự sự kiện, `clarify`/`refuse`, 429, 503 trước stream, sự kiện `error` giữa stream.
**Phụ thuộc:** BE-9, SP-1.

### OPS-2 (phần agent). Dashboard + cảnh báo LLM
Dashboard và rule cảnh báo > 25 RPM (tính cả lượt dịch) trong `Pipeline/OracleVM/VM2/monitoring/grafana/`. Nạp key PROD do chủ dự án làm.

---

## R2 — Số đo nước + cảnh báo độc tố (không cần AI, chạy song song R0/R1)

### BE-11. Test AquariumService (làm được ngay)
Chỉ thêm test cho Create / AddFish / RemoveFish / Delete (hiện 5 test). Không đổi hành vi; thấy lỗi thì mở việc riêng. Lý do: ADR-0003 yêu cầu có test trước khi đổi cách lưu. (Số đo nước là bảng riêng, chỉ thêm dòng; mô phỏng thả cá chỉ đọc — nên R2/R3 không chạm điều kiện mở lại ADR-0003.)

### BE-12. Bảng số đo nước + API
Entity `WaterReading` (bể, người dùng, thời điểm đo, NH₃/NO₂/NO₃/pH/nhiệt độ có thể trống, ghi chú). Repository riêng — **không** đi qua `GenericRepository.UpdateAsync` của Aquarium (hàm này đánh dấu cả danh sách cá là đã sửa). API theo C-R2, kiểm chủ bể.
**Phụ thuộc:** C-R2, BE-11.

### BE-13. Luật cảnh báo nước
Hàm thuần `IWaterAlertRules.Evaluate(loại nước, lịch sử)` → cảnh báo. **Ngưỡng lấy từ nguồn nuôi cá ngoài FishBase, có trích dẫn** (FishBase không có ngưỡng nitrit/nitrat; amoniac chỉ ~41 dòng). Tách luật nước ngọt / mặn. Luật ngưỡng + xu hướng. Thiếu chỉ số → bỏ qua luật đó.
**Xong khi:** test theo bộ ca thử (biên, loại nước Unknown, lịch sử rỗng, xu hướng); mọi kết quả có nguồn + câu "tham khảo, không thay thế xét nghiệm".
**Phụ thuộc:** C-R2 (bảng ngưỡng từ DEC-R2).

### BE-14. Đẩy cảnh báo qua push
Cảnh báo danger → gửi push qua `IWebPushNotifier` có sẵn, tối đa 1 lần / bể / chỉ số / 12 giờ. Gợi ý thay nước → đề xuất tạo nhắc nếu chưa có (không tự tạo). Khi có kênh push mobile (BE-22) thì đi qua cùng interface.
**Phụ thuộc:** BE-12, BE-13.

---

## R3 — Mô phỏng thả thêm cá

### BE-15. Trait loài ở FishDex
Lưu trait theo schema chủ dự án chốt (DEC-R3), từ FishBase (`stocks`: nhiệt độ, pH, dH; chiều dài tối đa; kiểu ăn) và cá cộng đồng (`SpeciesSnapshot`). Quy tắc dữ liệu: **gộp nhiều stock** theo quy tắc đã chốt; **bỏ khoảng nhiệt độ rộng hơn 15°C** khỏi luật (đánh dấu "biên rộng"); tách nước ngọt / mặn; cờ boolean kiểu Access (0 = "không" hoặc "chưa ghi nhận") **không** được hiểu là "không". Endpoint nội bộ đọc trait, nhận trait trích xuất từ repo AI (không ghi đè nguồn gốc, phải qua duyệt), báo cáo độ phủ. Không đưa snapshot FishBase trở lại database (ADR-0001).

### BE-16. Analyzer + endpoint mô phỏng
`IAquariumAnalyzer` với các "case" cắm thêm: **nhiệt độ / pH / dH chồng lấn, kích thước trưởng thành so với thể tích bể, kiểu ăn (ăn mồi)**. Tính hung dữ và nhóm tối thiểu **chỉ bật khi có nguồn bổ sung** (đóng góp cộng đồng hoặc trích xuất đã duyệt). Mỗi case trả điểm + giải thích + độ chắc chắn; dưới ngưỡng "đủ dữ liệu" (hiện ~26% loài cá cảnh đủ nhiệt độ + pH) → "chưa đủ dữ liệu", không chấm điểm. Endpoint **chỉ đọc** `POST /aquahome/v1/aquariums/{id}/fish:simulate`. Thêm case = thêm class + test, không sửa phần lõi.
**Phụ thuộc:** BE-15.

---

## R4 — Tìm ảnh (song song, bắt đầu được sau BE-1)

### BE-17. Vector ảnh ở FishDex
Ảnh là `SystemImage` (không có entity `SpeciesMedia`). Tạo bảng riêng `SystemImageEmbedding` (vector 512, HNSW) thay vì thêm cột. Endpoint nội bộ: liệt kê ảnh cần embed (URL tạm qua `pic.ObjectKey`), ghi vector, tìm theo vector. Code tìm ảnh tách riêng code tìm chunk.

### BE-18. AquaHome: API tìm ảnh
Trừ quota `ImageSearchPerDay` → gửi ảnh tới VM3 lấy vector → nhờ FishDex tìm → trả kết quả. Không lưu ảnh người dùng. Giới hạn 5MB. Lỗi → 503. p95 < 700ms.
**Phụ thuộc:** BE-17, AI-6.

---

## R5 — Chuẩn bị cho app mobile (ADR 0006; không phụ thuộc AI, làm sớm được)

### BE-19. Bật validate audience (ADR-0004, checklist PROD #4)
Đăng ký FishDex và AquaHome là resource server trong OpenIddict để token mang audience riêng; bật `ValidateAudience` **ở một chỗ** (`ResourceServerExtensions`). Kiểm web vẫn đăng nhập và gọi được cả 2 service.
**Xong khi:** token cấp cho service A bị service B từ chối; test cho cả 2 scheme.

### BE-20. Sửa discovery OIDC trả host nội bộ
Ở local, `/.well-known/openid-configuration` trả `authorization_endpoint` là `usermanagement-api:8080`. Web không đọc discovery nên chưa lộ ra, nhưng AppAuth của mobile thì đọc. Cấu hình issuer / URL công khai đúng cho từng môi trường; kiểm trên PROD.
**Xong khi:** discovery ở mọi môi trường trả URL công khai đúng.

### BE-21. Client OAuth cho mobile
Đăng ký client riêng (public client, PKCE, redirect `fishlover://callback`, scope như web + `offline_access`). Theo hợp đồng C-MB.

### BE-22. Kênh push cho mobile
Sau khi chủ dự án chọn (DEC-MB: Expo Push hay FCM/APNs): lưu token thiết bị, gửi qua cùng interface với Web Push để các nơi đang gửi (nhắc việc, cảnh báo nước) không phải sửa.

---

## Thứ tự gợi ý cho BackEnd

1. F1 → (song song) F5, F8, BE-11, BE-19, BE-20
2. BE-1 → BE-2, BE-3 → BE-4 → BE-5 (khi C-R0 và seed alias chốt)
3. BE-7 (khi ADR duyệt) → BE-8 → BE-9 → BE-10 (sau SP-1); BE-6
4. BE-12, BE-13 (khi C-R2 chốt) → BE-14
5. BE-21, BE-22 khi app mobile bắt đầu
6. BE-17 khi rảnh (làn R4) → BE-18
7. BE-15 → BE-16
