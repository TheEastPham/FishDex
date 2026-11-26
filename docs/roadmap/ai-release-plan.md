# Kế hoạch phát hành AI v2.0 — hợp nhất roadmap học tập với cách làm hiện tại

> Nguồn gốc: roadmap học "AI Learning Roadmap — Unified" (cây Root → Nhánh 1/2/3) và "Epic 2: AI Q&A Service" trên Notion. **File này tự đủ để lập kế hoạch, không cần Notion**: các mã Root/Nhánh/Story được giải nghĩa ở mục [Bảng mã](#bảng-mã).
> Tài liệu này KHÔNG thay roadmap học; nó thêm lớp **phát hành** (thứ tự, tiêu chí hoàn thành, mobile) lên trên. Kế hoạch sống trong repo, không đồng bộ lên Notion theo quyết định của chủ dự án.
> Quyết định đã chốt với chủ dự án: Q1 Nhánh 1 trước · Q2 số đo nước nhập tay trước · Q3 bể 3D tách track riêng · ~~Q4 PWA trước, Capacitor khi có lý do~~ → **thay bằng ADR 0006 (26/11/2026): app mobile native bằng Expo** · Q5 (24/11/2026) câu hỏi được quy về tiếng Anh trước khi embed bằng MiniLM, chunk giữ tiếng Anh, tên Việt đi qua bảng alias · Q6 (26/11/2026) dịch vụ ML Python ở repo riêng `FishLover-AI`, phần tích hợp .NET ở repo này (ADR 0006).
> Số liệu dữ liệu làm cơ sở cho các điều chỉnh bên dưới: [`r0-data-analysis.md`](r0-data-analysis.md). Cập nhật lần cuối: 27/11/2026.

## Bối cảnh cho người lập kế hoạch (đọc trước)

Dành cho người (hoặc AI) lần đầu lập kế hoạch chi tiết từ file này.

**Hiện trạng (27/11/2026):** **chưa có dòng code AI nào chạy**. Production đang chạy `release/v1.2`. Những thứ đã có sẵn để dựa vào:
- FishDex: extension `vector` (pgvector 0.8) được bật bằng script khởi tạo DB (`Pipeline/local/FishDex/init.sql`, `Pipeline/OracleVM/VM1/services/init-db.sh`), **không** bằng EF migration. Code hiện **không có entity vector nào**.
- **Phải kiểm trước R0:** DB FishDex local có sẵn bảng `SpeciesChunks` (`Id`, `SpecCode`, `ChunkIndex`, `Embedding vector`, `ChunkText`, 0 dòng) do migration `20260527151440_AddSpeciesChunks` tạo — migration này vẫn nằm trong `__EFMigrationsHistory` nhưng **không còn trong code**. Kiểm PROD có bảng này không; entity `SpeciesChunk` mới phải tái dùng hoặc bỏ bảng cũ một cách có chủ đích (migration mới), không được tạo trùng tên.
- AquaHome: `QuotaType.AiQaPerDay`, `ImageSearchPerDay`, `SearchPerDay` và `IQuotaService.ConsumeDailyAsync` (đếm theo ngày) đã có bảng, repo, seed — chưa nơi nào gọi.
- `Pipeline/Preprocessor/preprocess.py` (Python + DuckDB): bản chunk v1.0 từ parquet ra `output/species_chunks.jsonl` (bị gitignore). Nó đọc parquet ở `Pipeline/local/FishDex/parquetData/` (bản copy cục bộ, gitignore); R0 phải dựng lại chunk theo chủ đề (xem `r0-data-analysis.md`).
- `Pipeline/OracleVM/VM3/`: README + hai thư mục `embedding_service/`, `image_search_service/` còn rỗng.

**Đọc trước khi lập kế hoạch** (theo thứ tự):
1. `BackEndProject/CLAUDE.md` — lệnh, quy ước, mục "v2.0 AI Stack" (ràng buộc thiết kế chưa xây).
2. `BackEndProject/docs/adr/0004` (validate audience đang tắt), `0005` (Worker đăng video, một instance), `0006` (mobile Expo, repo AI riêng, hướng sau này).
3. `docs/architecture/c4/` — L2 Container và Deployment, phần 🚧 là kiến trúc đích.
4. `BackEndProject/GLOSSARY.md` — thuật ngữ; thêm từ mới theo skill `domain-modeling`.
5. [`r0-data-analysis.md`](r0-data-analysis.md) — độ phủ dữ liệu, lý do các điều chỉnh R0–R3.
6. `docs/guides/be-developer-guide.md`, `FrontEnd/CLAUDE.md` — cách thêm tính năng, test BE/FE.

**Code nằm ở đâu (ADR 0006):**

| Phần | Repo | Ghi chú |
|---|---|---|
| Dịch vụ ML Python (`/embed`, image search), script tạo chunk, notebook đánh giá, huấn luyện/thu gọn model | `FishLover-AI` (**chưa tạo**) | Khi tạo: chuyển `Pipeline/Preprocessor` và scaffold `Pipeline/OracleVM/VM3/*_service` sang; đường dẫn parquet đọc từ biến môi trường, không hard-code |
| `ILlmProvider`, `QueryNormalizer`, endpoint RAG, entity `SpeciesChunk` / `SpeciesAlias` / `ClipEmbedding` + migration, quota | FishDex (repo này) | Đi theo service sở hữu dữ liệu (FishDex) hoặc BFF (AquaHome) — xem "Việc mở" |
| UI chat, tìm ảnh, số đo nước | FishDex/FrontEnd (web), `FrontEnd/apps/mobile` (app 🚧) | Dùng chung `@fishlover/shared` |

**Quy tắc khi lập kế hoạch:**
- Mục [Việc mở](#việc-mở-cần-quyết-ở-vòng-hỏi-tiếp-theo) là câu **phải hỏi chủ dự án**, không tự chốt.
- Mỗi bản phát hành phải có tiêu chí hoàn thành đo được (bảng dưới); kế hoạch chi tiết chia thành lát mỏng có test, BE và FE tách commit.
- Phase nghiên cứu (R0, phần thử nghiệm của R1/R4) **chạy trên laptop cá nhân** của chủ dự án; VM3 chỉ bật khi cần cho PROD (mục VM3).

## Dữ liệu FishBase (không nằm trong repo)

Repo FishDex là public và **không chứa dữ liệu FishBase**; repo `FishLover-AI` cũng sẽ không chứa. Dữ liệu nằm trên laptop của chủ dự án, nơi chạy phase nghiên cứu.

| Thứ | Ở đâu | Là gì |
|---|---|---|
| `fishbase-v24.07/` | `D:\Workspace\FishLover\data\` (workspace, ngoài git; bản backup ở `D:\Workspace\_backups\`) | Bản phát hành FishBase v24.07 dạng **214 file parquet, ~214 MB**, mỗi file một bảng FishBase. Đọc bằng DuckDB hoặc pandas, chỉ đọc |
| `ExplainColumn.json` | cùng thư mục `data\` | Từ điển cột FishBase v24.07 (bản 16/10/2026) |
| `fishdex_public_Species.csv` | cùng thư mục `data\` | Phạm vi loài của FishDex, mỗi dòng một `SpecCode` |
| `exports/` | cùng thư mục `data\` | Các lô dữ liệu đã nạp lên PROD, mỗi lô có README |

- **Bảng hay dùng cho AI:** `species` (thông tin loài, `SpecCode`), `families`, `comnames` (tên thường gọi theo ngôn ngữ — tên Việt chỉ có ở 6,6% loài cá cảnh), `stocks` (khoảng nhiệt độ, pH, độ cứng; một loài có thể nhiều stock), `ecology`, `waterquality`. Danh sách đầy đủ cột và độ phủ: `r0-data-analysis.md`.
- **Phạm vi:** 3.751 loài cá cảnh là phạm vi làm trước (định nghĩa trong `r0-data-analysis.md`).
- **Nguồn tải:** chưa được ghi lại trong workspace. Cần tải lại thì hỏi chủ dự án; khi biết, ghi nguồn và giấy phép sử dụng của FishBase vào đây.
- **Ảnh loài** (cho CLIP, R4): không nằm trong parquet; ảnh đang ở bucket R2 `system-image` (key `{SpecCode}/{Id}{ext}`, xem `SystemImage.ObjectKey`). Batch embed ~70k ảnh chạy trên laptop.

## Bảng mã

Các mã trong cột "Học trước" và tên Story đến từ Notion; nghĩa của chúng:

| Mã | Nghĩa |
|---|---|
| Root·W | Khởi động Python cho dev .NET: môi trường, đọc parquet bằng pandas, gọi model có sẵn |
| Root·0 | Khái niệm AI đủ dùng: embedding, tìm theo nghĩa, RAG, CLIP, vì sao chia chunk |
| Root·E | Bộ đo dùng chung: bộ loài lõi, bộ câu hỏi chuẩn, `eval.py` |
| Root·D | Dữ liệu đúng phạm vi cá cảnh: định nghĩa phạm vi, đo độ phủ, chunk theo nhóm câu hỏi, bảng tên gọi |
| Root·1 | Embedding và vector: MiniLM, embed toàn bộ trên laptop, nạp pgvector, service `/embed` |
| Root·2 | LLM core: gọi Groq, đổi sang Ollama cùng code, system prompt tiếng Việt |
| Nhánh 1.1 | RAG lõi: hỏi thẳng LLM làm mốc → tra hồ sơ + LLM → retrieval pgvector → chuyển sang .NET → cache, UI, quota (1.2 = nâng chất lượng) |
| Nhánh 2.1 | Tìm loài bằng ảnh: CLIP trên ảnh mẫu → batch ~70k ảnh vào pgvector → API + UI (fine-tune để sau) |
| 3.0 | Analyzer bằng luật: case viết cứng, chưa cần LLM |
| 3.1 | Trích xuất trait bù chỗ FishBase thiếu (LLM, có kiểm duyệt), lưu trait linh hoạt (JSONB) |
| 3.2 | Explanation Layer: LLM chỉ diễn giải kết quả của luật |
| 3.4 | Time-series: dự đoán từ lịch sử số đo |
| Story 2.0 | Ingest chunk từ parquet vào pgvector, chạy lại không trùng |
| Story 2.1 | Embedding service `/embed` (FastAPI, MiniLM 384-d) |
| Story 2.2–2.6 | Pipeline RAG, `ILlmProvider` (Groq/Ollama), cache, chat FE + SSE + trích dẫn, quota `aiQaPerDay` |
| Story 4.1–4.4 | CLIP service, index ảnh pgvector (512-d), API tìm ảnh ở FishDex, UI tìm ảnh |

Nội dung khoá học tương ứng (bài, số đo) ở `research/ai-course/` của workspace; bản tách Story cũ (06/2026, một phần đã lỗi thời) ở `_archive/fishdex-ai-option-a-2026-06/ai-option-a-stories.md`. Khi hai nơi đó mâu thuẫn với file này, **file này thắng**.

## Nguyên tắc

1. **Học vừa đủ, đúng lúc.** Mỗi bản phát hành chỉ kéo đúng các node Root/Nhánh nó cần (cột "Học trước"). Không đọc hết trước khi làm.
2. **Quy tắc trước, LLM sau.** Case có công thức hoặc luật rõ (độc tố, tương thích) chạy bằng rule; LLM chỉ diễn giải kết quả (Explanation Layer, 3.2). Không ép AI vào chỗ rule đã đủ.
3. **Mọi đầu ra AI có độ chắc chắn.** Dữ liệu FishBase nhiều cột null → trả `Score + Explanation + Confidence`, thiếu dữ liệu thì nói thiếu.
4. **Cổng chất lượng theo bản phát hành**, không theo ngày: mỗi bản có tiêu chí hoàn thành đo được (bên dưới).
5. **Ba làn song song, độc lập**: A (Q&A → Analyzer), B (Image Search), C (giao sản phẩm/mobile — app Expo, ADR 0006).

## Các bản phát hành

| # | Bản | Nội dung | Học trước (xem Bảng mã) | Tiêu chí hoàn thành |
|---|---|---|---|---|
| **R0** | Nền dữ liệu + embedding + chuẩn hóa câu hỏi | Story 2.0 (ingest chunk từ parquet, upsert chạy lại được; **dựng lại chunk theo chủ đề**, 3.751 loài cá cảnh trước), 2.1 (FastAPI `/embed` trên VM3, cache 1h, VM3 down → 503), **bảng `SpeciesAlias` (Việt/Anh/khoa học → SpecCode) + `QueryNormalizer` chạy bằng luật** (xem mục "Chuẩn hóa câu hỏi") | Root·W (tùy chọn), Root·0, Root·1 | Script re-run không trùng; `/embed` đạt ngân sách; có **bộ câu hỏi chuẩn ~30 câu tiếng Việt** kèm loài đúng; đo được **từng tầng** (độ chính xác alias → hit@k retrieval → đúng/từ chối đúng) chứ không chỉ điểm cuối |
| **R1** | AI Q&A | Story 2.2–2.6: pipeline chuẩn hóa → lọc theo SpecCode/chunkType → vector → LLM sinh câu trả lời tiếng Việt; `ILlmProvider` (Groq/Ollama/Gemini), cache Redis, chat FE + SSE + trích dẫn, quota `aiQaPerDay`. LLM dịch chỉ là fallback có cache; tên mơ hồ → hỏi lại người dùng | Root·2, Nhánh 1.1 | p95 < 1,5s; Groq 429 → 503; **tỷ lệ trả lời đúng/trích dẫn đúng trên bộ chuẩn** đạt ngưỡng đã chốt; **từ chối đúng** nhóm câu thiếu dữ liệu; tỷ lệ gọi LLM dịch đo được và nằm trong ngân sách RPM; metric RPM lên Grafana |
| **R2** | Số đo nước + cảnh báo độc tố | Entity số đo nước (NH₃/NO₂/NO₃/pH/nhiệt độ, **nhập tay**), luật ngưỡng chung + xu hướng (ngưỡng lấy từ nguồn nuôi cá có trích dẫn — FishBase không có ngưỡng nitơ theo loài, xem `r0-data-analysis.md` mục 3), nhắc thay nước qua Web Push/Worker, gate theo role/quota | 3.0 (bản nhẹ), Root·0 | Cảnh báo kích hoạt đúng với bộ ca thử; luôn hiện "tham khảo, không thay thế xét nghiệm"; không cần LLM để chạy (LLM chỉ giải thích, tùy chọn) |
| **R3** | Mô phỏng thả thêm cá | Schema trait (EAV/JSONB), trích xuất bù chỗ FishBase thiếu (3.1), `IAquariumAnalyzer` + case tương thích: nhiệt độ/pH/dH chồng lấn, kích thước trưởng thành vs thể tích, kiểu ăn. **Hung dữ và nhóm tối thiểu cần nguồn bổ sung** (đóng góp cộng đồng hoặc trích xuất có kiểm duyệt, 3.0/3.1) vì FishBase không có tính cách và cờ sống bầy chỉ dương tính ở 2,3% loài cá cảnh | 3.0, 3.1, 3.2 | Báo cáo độ phủ trait; chỉ chấm điểm khi đủ dữ liệu (hiện ~26% loài cá cảnh đủ nhiệt độ + pH); mọi kết quả có Confidence; contract test cho analyzer; thêm case mới không sửa foundation |
| **R4** | Image Search (làn B) | Epic 4: CLIP trên VM3, `ClipEmbedding` 512-d, tìm ảnh | Root·1, Nhánh 2.1 | p95 < 700ms; VM3 down → 503. Làm song song, chèn khi rảnh tay, không chặn R1–R3 |
| **R5** | Mobile (làn C) | App iOS + Android bằng **Expo** ở `FrontEnd/apps/mobile` (ADR 0006). Trước tiên 5 việc chuẩn bị trong ADR 0006: validate audience, sửa discovery OIDC, client OAuth cho mobile, tách phần chỉ web khỏi `@fishlover/shared`, chọn kênh push. Sau đó làm các luồng chính của web, rồi chat AI/tìm ảnh khi R1/R4 xong | — | Build qua EAS cho cả hai nền tảng; đăng nhập PKCE bằng client riêng, token trong SecureStore; checklist iPhone 12+ đạt; qua duyệt App Store và Google Play; API giữ tương thích `/v1` |
| **R6** | Bể 3D | Hiển thị 1 bể (hộp bể + cá theo kích thước thật, nhóm đàn), gate theo role | — | Chạy mượt trên iPhone 12; bản đơn giản trước, không đua đồ họa |
| **R7** | Time-series (gated) | Dự đoán tần suất thay nước, nguy cơ bệnh | 3.4 | Chỉ mở khi đủ lịch sử số đo + nhãn thật từ người dùng |

Thứ tự khuyến nghị: **R0 → R1** (cam kết, dựng hạ tầng dùng chung) → **R2** (giá trị cao, rủi ro thấp, không phụ thuộc LLM) → **R3** → R5/R6; R4 chèn song song. R7 chờ dữ liệu.

## Cách dùng các skill trong mỗi bản phát hành

Mỗi bản đi qua cùng một vòng; skill nào nằm ở bước nào:

1. **Chọn lát cắt** — `grilling` trên các nhánh chưa quyết của bản đó (mỗi vòng có đề xuất sẵn).
2. **Chốt ngôn ngữ và quyết định** — `domain-modeling`: thêm thuật ngữ vào `GLOSSARY.md` (ví dụ "Water reading", "Analyzer", "Confidence"); ghi ADR khi quyết định khó đảo, gây bất ngờ và có đánh đổi thật.
3. **Chọn ranh giới** — `codebase-design`: seam cho `ILlmProvider`, `IAquariumAnalyzer`, gateway tới VM3 (tái dùng bài học "Unavailable ≠ Empty" của FishDex gateway). Ưu tiên module sâu, test qua interface.
4. **Làm theo lát mỏng có test** (test pure trước; test cần DB dùng Testcontainers như `FishDex.Tests`).
5. **Rà soát** — `code-review` rồi `security-review` (đặc biệt R1: prompt injection qua chunk, lộ khoá Groq; R2: dữ liệu sức khỏe bể của người dùng).
6. **Cuối mỗi làn/2–3 bản** — `improve-codebase-architecture` để tìm module nông phát sinh, rồi `simplify`.
7. **Ghi lại** — cập nhật trạng thái trong tài liệu này (kế hoạch không đồng bộ lên Notion) và memory dự án.

## Chuẩn hóa câu hỏi (Việt → Anh) trước khi tìm

**Quyết định (chủ dự án, 24/11/2026):** câu hỏi được quy về tiếng Anh rồi mới embed bằng MiniLM; chunk giữ nguyên tiếng Anh. Lý do: MiniLM là model tiếng Anh, và tên Việt chỉ có ở 6,6% loài cá cảnh nên embedding tiếng Việt không nối được tên với loài.

Module `QueryNormalizer` (seam giữa câu hỏi thô và retrieval), các bước theo thứ tự, **luật trước, LLM sau**:

1. **Làm sạch**: chữ thường, bỏ dấu để so khớp (giữ bản có dấu để hiển thị), gộp khoảng trắng.
2. **Tìm alias loài (không LLM)**: khớp n-gram với bảng `SpeciesAlias` gồm tên Việt FishBase, tên cộng đồng đã duyệt (`CommunityCommonName`), tên Anh (`FBname`), tên khoa học và một danh sách tên chơi cá do chủ dự án duyệt (seed). Kết quả: 0, 1 hoặc nhiều SpecCode.
3. **Nhận diện chủ đề (không LLM)**: từ điển khoảng 20 chủ đề (ăn uống, nhiệt độ, pH/độ cứng, kích thước, tuổi thọ, nuôi chung, sinh sản, bệnh, nguồn gốc/phân bố) → từ khóa tiếng Anh + `chunkType`.
4. **Dựng câu truy vấn tiếng Anh** từ loài + chủ đề. Có loài thì retrieval **lọc cứng theo SpecCode (và chunkType)** rồi mới xếp hạng bằng vector — chính xác hơn tìm vector toàn cục.
5. **Phần không hiểu được** (không có alias, không có chủ đề) → LLM dịch/trích xuất (fallback), cache theo văn bản đã chuẩn hóa. Không dịch được thì hỏi lại người dùng, không đoán.
6. **Mơ hồ** (nhiều SpecCode, ví dụ "cá chép") → trả danh sách ứng viên để người dùng chọn, không tự chọn.

Câu trả lời vẫn được LLM sinh bằng tiếng Việt từ chunk tiếng Anh; phần trích dẫn hiện tên Việt/Anh/khoa học của loài.

**Đánh đổi phải nhớ:**
- Mỗi lần gọi LLM dịch tốn thêm một request Groq. Với 30 RPM, nếu mọi câu đều dịch bằng LLM thì còn ~15 câu hỏi/phút; độ trễ cộng thêm có thể phá ngân sách p95 < 1,5s (ngân sách hiện đã ~1,35s). Vì vậy LLM chỉ là fallback, có cache, và tỷ lệ fallback là một chỉ số phải đo.
- **Chất lượng chốt ở bảng alias.** Alias sai nghĩa là trả lời sai loài một cách tự tin. Cần ngưỡng mơ hồ, Confidence và bộ test riêng cho alias.
- Bảng alias là tài sản dữ liệu mới: cần chủ sở hữu (FishDex, vì nó sở hữu tên loài) và quy trình bổ sung (tận dụng tính năng đóng góp tên cộng đồng đã có).

**Phép đo baseline ở R0 (không bắt buộc áp dụng):** so sánh ba đường trên cùng bộ câu hỏi chuẩn — (a) câu Việt thô → MiniLM, (b) chuẩn hóa → MiniLM (**đã chọn**), (c) một model đa ngôn ngữ cùng 384 chiều (ví dụ `paraphrase-multilingual-MiniLM-L12-v2`; kích thước và tốc độ trên ARM chưa kiểm chứng). Mục đích là có số liệu thay vì phỏng đoán; nếu (b) không hơn (c) rõ rệt thì lúc đó mới xem lại quyết định.

## VM3 — khi nào bật, cần cài gì

**Hiện trạng:** VM3 là Ubuntu **ARM (aarch64)**, 6GB RAM (Oracle Always Free), do chủ dự án xác nhận. README ở `Pipeline/OracleVM/VM3/` vẫn ghi *Specs: TBD*, trạng thái đang phát triển, hai thư mục `embedding_service/` và `image_search_service/` còn rỗng. Chưa xác nhận: phiên bản Ubuntu, số OCPU, IP.

**Code ở đâu:** code của hai dịch vụ viết trong repo `FishLover-AI` (ADR 0006), build ra image `linux/arm64`. `Pipeline/OracleVM/VM3/` trong repo này chỉ còn giữ phần hạ tầng (compose, README) nếu muốn; hai thư mục scaffold rỗng chuyển đi khi tạo repo AI.

**Hệ quả của ARM cho thiết kế:**
- Image phải là `linux/arm64`. Laptop dev là x86 Windows, nên dựng image cho VM3 bằng `docker buildx build --platform linux/arm64` (QEMU, chậm) hoặc build ngay trên VM3 / trong CI chạy ARM. Dev local vẫn chạy x86 bình thường vì code Python giống nhau.
- Chọn base image đa kiến trúc (ví dụ `python:3.12-slim`), tránh wheel chỉ có cho x86. Trước khi chốt `requirements.txt`, kiểm tra `torch`, `sentence-transformers`, `open_clip_torch`, `pillow`, `numpy` có bản aarch64 (dùng bản PyTorch CPU).
- Tốc độ suy luận trên lõi ARM thường chậm hơn laptop Ryzen: phải **đo trên chính VM3**, không suy từ số liệu laptop (MiniLM ~30ms/câu và CLIP ~200ms/ảnh trong Notion là số của laptop). Nếu không đạt ngân sách, cân nhắc xuất ONNX hoặc giảm batch trước khi nghĩ tới phần cứng khác.
- Batch embed 70k ảnh CLIP vẫn nên chạy trên laptop rồi ghi vector vào pgvector; VM3 chỉ phục vụ truy vấn live.

**Khi nào bật:**
| Mốc | Làm gì | VM3 |
|---|---|---|
| R0 | Viết Story 2.0 (ingest) và 2.1 (FastAPI `/embed`) | **Chưa cần.** Chạy hoàn toàn trên laptop (CPU), đúng quyết định "dev local, chỉ deploy VM3 khi cần production" |
| Cuối R0 / trước R1 lên PROD | Cần AquaHome PROD gọi được `/embed` | **Bật VM3 lúc này.** Chạy checklist bên dưới |
| R4 (Image Search) | Story 4.1 | Thêm `image_search_service` :8001 vào cùng VM3 (kiểm tra RAM: ≈3,5GB / 6GB) |

**Checklist trước khi bật (bạn chạy tay, tôi hướng dẫn từng bước):**
1. Xác nhận cấu hình: `uname -m` (mong đợi `aarch64`), `nproc`, `free -h`, `lsb_release -a`. Thử `pip install` thật `torch`, `sentence-transformers`, `open_clip_torch` trong một venv/container arm64 trước khi hứa tiến độ.
2. Cài Docker + Compose; thêm swap vài GB để an toàn lúc nạp model.
3. Mạng: VM3 chỉ nhận cổng 8000/8001 **từ VM1** (security list / NSG của VCN); không mở public. Mạng Docker khớp `fishlover_prod` như README.
4. Triển khai `embedding_service` bằng compose; model tải lúc khởi động (~120MB), mount volume để không tải lại mỗi lần.
5. Kiểm tra: `curl localhost:8000/health`, một lần `POST /embed`, đo độ trễ so với ngân sách (embedding ≈ 50ms).
6. Giám sát: thêm `:8000/metrics` vào scrape Prometheus ở VM2; cảnh báo khi down.
7. Kiểm thử đường lỗi: tắt VM3 rồi gọi từ AquaHome phải trả 503, không fallback.
8. Lưu ý chính sách Oracle Always Free về instance nhàn rỗi; theo dõi để máy không bị thu hồi.

## Ràng buộc kỹ thuật phải giữ (từ CLAUDE.md và Notion)

- Groq free 30 RPM → alert > 25 RPM; 429 → 503, không retry tự động. Fallback Ollama/Gemini qua `ILlmProvider`. Mỗi câu hỏi tốn tối thiểu 1 request (sinh câu trả lời) và thêm 1 nếu phải dịch bằng LLM — tính vào ngân sách RPM.
- VM3 (6GB): embedding + CLIP ≈ 3,5GB; không expose public; down → 503, không fallback embedding local.
- pgvector: `SpeciesChunk.Embedding` (384) và `SpeciesMedia.ClipEmbedding` (512) không trộn trong một truy vấn.
- Ngân sách: RAG p95 < 1,5s; Image search p95 < 700ms.
- BE và FE tách commit khi làm feature; mobile 390px là mặc định; mọi chuỗi qua i18n (4 ngôn ngữ vi/en/de/zh).
- API thêm cho AI phải dùng được cho cả web lẫn app mobile và giữ tương thích ngược trong `/v1` (app đã cài không cập nhật cùng lúc).
- Mọi object key qua `FishDexObjectKeys` / `AquaHomeObjectKeys`; không tự ghép.

## Rủi ro và cách chặn

| Rủi ro | Biện pháp |
|---|---|
| Cảnh báo độc tố sai gây hại cá | Chỉ là tham khảo, ngưỡng lấy từ nguồn nuôi cá tin cậy và ghi nguồn; có bộ ca thử; không tự động hành động |
| FishBase thiếu trait → kết luận sai | Confidence + báo cáo độ phủ (nhiệt độ+pH ~26%, sống bầy 2,3% loài cá cảnh); không đưa ra điểm khi dưới ngưỡng dữ liệu |
| LLM bịa | RAG có trích dẫn; bộ câu hỏi chuẩn đo định kỳ; từ chối khi không có ngữ cảnh |
| Alias sai/mơ hồ → trả lời sai loài | Khớp nhiều loài thì hỏi lại; Confidence; bộ test alias; chỉ lọc cứng theo SpecCode khi khớp chắc chắn |
| Dịch bằng LLM làm hết quota/chậm | Luật trước, LLM fallback có cache; đo tỷ lệ fallback; ngân sách RPM tính cả request dịch |
| Tên Việt thiếu (6,6% loài cá cảnh) | Bảng alias lấy thêm tên Anh, tên khoa học, tên cộng đồng và seed tên chơi cá; mở rộng bằng tính năng đóng góp tên |
| Quá tải phạm vi | Mỗi bản có tiêu chí hoàn thành; 3D và time-series bị gate |
| Chi phí/quota LLM | Cache Redis theo hash(question + chunk ids); Ollama cho dev |

## Việc mở (cần quyết ở vòng hỏi tiếp theo)

- **Chuẩn hóa câu hỏi:** `QueryNormalizer` đặt ở đâu — endpoint resolve trong FishDex (sở hữu tên loài, AquaHome BFF gọi qua gateway) hay trong AquaHome? Model LLM dịch fallback là gì và độ trễ thực tế có nằm trong ngân sách không?
- **Bảng alias:** ai duyệt danh sách seed tên chơi cá; quy tắc xử lý tên mơ hồ; có dùng tên Anh `FBname` làm alias mặc định không.
- **Dữ liệu:** quy tắc gộp nhiều `stock` của một loài; loại khoảng nhiệt độ >15°C khỏi luật; tách luật nước ngọt/mặn.
- Ngưỡng chất lượng cụ thể cho R1 (tỷ lệ đúng và từ chối đúng trên bộ chuẩn) và nguồn ngưỡng độc tố cho R2.
- Role nào được dùng R2/R3/R6, và quota đi theo `RoleQuota` hiện có hay loại mới.
- Số đo nước lưu ở service nào (AquaHome gần nhất) và giữ bao lâu.
- **Sau v2.0 (ghi ngày 26/11/2026, chưa lên lịch):** mô hình AI nhỏ chạy ngay trên app mobile (~1 tỷ tham số, nén 4-bit, tải về khi bật tính năng) và kho ảnh/video trên máy dùng được khi không có mạng. Ràng buộc và nơi làm: mục "Hướng sau này" của `BackEndProject/docs/adr/0006-mobile-expo-chung-repo-ai-tach-repo.md`.
