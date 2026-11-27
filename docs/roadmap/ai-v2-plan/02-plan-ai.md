# Plan AI — repo private `FishLover-AI`

> Repo mới: `TheEastPham/FishLover-AI` (private, ADR 0006). Chứa **toàn bộ code Python AI**.
> Đọc kèm [00-plan-tong.md](00-plan-tong.md) (quyết định, hợp đồng) và [`../r0-data-analysis.md`](../r0-data-analysis.md) (số liệu dữ liệu). Mỗi mục có mã sẽ thành 1 ticket.

## Repo này làm gì, không làm gì

**Làm:**
- 2 service chạy trên VM3: embedding (:8000, R0) và tìm ảnh CLIP (:8001, R4). CI của repo build image `linux/arm64` và đẩy lên GHCR.
- Job offline: dựng chunk theo chủ đề từ parquet, nạp chunk + vector, backfill vector cho ~70k ảnh, trích trait bù (R3).
- Bộ đánh giá chất lượng **theo tầng** (alias → chủ đề → retrieval → câu trả lời → hệ thống) và phép đo baseline.
- Notebook nghiên cứu. Sau v2.0: huấn luyện / thu gọn model nhỏ cho điện thoại (ADR 0006, "Hướng sau này").

**Không làm:**
- Không ghi database. Mọi dữ liệu vào FishDex qua endpoint `api/internal/**` với `X-Internal-Api-Key`. Repo không giữ mật khẩu database.
- Không xác thực người dùng, không giữ quota, không phục vụ web/app trực tiếp. Chỉ service .NET gọi vào, bằng IP private.
- Không gọi LLM ở đường online — đó là việc của AquaHome. Job offline (trích trait) được phép gọi LLM.
- **Không chứa dữ liệu FishBase.** Parquet nằm trên laptop chủ dự án; đường dẫn lấy từ biến môi trường.
- Không giữ compose triển khai VM3: phần hạ tầng ở monorepo `Pipeline/OracleVM/VM3` (cạnh VM1/VM2), kéo image từ GHCR về chạy.

## Bối cảnh kỹ thuật

- **Dữ liệu:** FishBase v24.07, 214 file parquet (~214 MB) ở `D:\Workspace\FishLover\data\fishbase-v24.07` (ngoài git), kèm `ExplainColumn.json`, `fishdex_public_Species.csv`. Đọc bằng DuckDB, **chỉ đọc**, luôn từ parquet (không dùng csv cho dữ liệu FishBase).
- **Phạm vi làm trước:** 3.751 loài cá cảnh (`species.Aquarium` khác `never`/`rarely`/rỗng).
- **Ngôn ngữ:** chunk **toàn tiếng Anh**. Câu hỏi tiếng Việt được FishDex quy về tiếng Anh trước khi embed (Q5). MiniLM là model tiếng Anh.
- **VM3:** Ubuntu ARM aarch64, 6GB RAM. Laptop dev là x86 Windows → image phải build `linux/arm64`. Ngân sách RAM embedding + CLIP ≈ 3,5GB / 6GB. Không chạy LLM trên VM3. Số đo latency phải đo lại trên VM3.
- **Phase nghiên cứu** (R0, phần thử của R1/R4) chạy trên laptop chủ dự án; VM3 chỉ bật khi cần cho PROD.

## Cấu trúc đề xuất

```
FishLover-AI/
  services/
    embedding/        # FastAPI, all-MiniLM-L6-v2, 384 chiều
    image_search/     # FastAPI, open_clip ViT-B-32, 512 chiều
  jobs/
    chunks/           # dựng chunk theo chủ đề (thay preprocess v1.0)
    ingest_chunks/
    clip_backfill/
    trait_extract/
  eval/               # bộ câu hỏi chuẩn, đo theo tầng, baseline
  notebooks/
  openapi/            # hợp đồng do FastAPI sinh, commit lại
  tests/fixtures/     # parquet mẫu rất nhỏ (vài loài) để test chạy trên CI
  CLAUDE.md
```

Công cụ: Python 3.12, `uv`, `ruff`, `pytest`; image gốc `python:3.12-slim`; PyTorch bản CPU. Không commit model, `.env`, `.venv`, dữ liệu FishBase.

---

## F — Nền móng

### F6. Khởi tạo repo
**Làm:**
- Tạo cấu trúc trên; `CLAUDE.md` ghi ranh giới (không ghi DB, gọi FishDex qua internal endpoint, không commit model hay dữ liệu, đọc parquet).
- CI GitHub Actions: `ruff check`, `pytest`, build image `linux/arm64`; push lên GHCR khi merge `main`.
- Chuyển `Pipeline/Preprocessor` (bản v1.0, để so sánh) và 2 thư mục scaffold rỗng `Pipeline/OracleVM/VM3/*_service` sang. Sau đó mở PR ở monorepo xoá chúng, README trỏ sang repo mới.
**Xong khi:** CI xanh; preprocess v1.0 cho đúng 8.883 chunk trên cùng dữ liệu.
**Phụ thuộc:** F7 (chủ dự án tạo repo).

### AI-0. Kiểm thư viện trên arm64 (làm ngay sau F6)
**Vì sao:** rủi ro lớn nhất của cả làn AI. Nếu thư viện không có bản aarch64 thì mọi tiến độ đều sai.
**Làm:** trong container `linux/arm64` (QEMU trên CI), cài thật `torch` (CPU), `sentence-transformers`, `open_clip_torch`, `pillow`, `numpy`; chạy embed 1 câu và 1 ảnh mẫu; ghi phiên bản + thời gian vào README.
**Xong khi:** có kết luận rõ: cài được / phải đổi cách (ONNX, phiên bản khác).

---

## R0 — Chunk, embedding, nạp dữ liệu, bộ đánh giá

### AI-1. Embedding service
**Làm:**
- FastAPI đúng hợp đồng C-R0: `GET /health`, `POST /embed`, `POST /embed/batch`, `GET /metrics`.
- Model nạp 1 lần lúc khởi động vào `models/` (mount volume). Chưa nạp xong → 503.
- Kiểm `X-Internal-Api-Key` (so sánh an toàn thời gian); key trống → từ chối tất cả.
- Giới hạn: câu ≤ 2000 ký tự, batch ≤ 64. Metric Prometheus: request, độ trễ, RAM.
**Xong khi:** test với model giả phủ các trường hợp lỗi; test chậm (chạy tay) với model thật cho vector 384 đã chuẩn hoá; `openapi/embedding.v1.yaml` khớp schema FastAPI sinh ra; README có lệnh đo p95 để chạy trên VM3.
**Phụ thuộc:** F6, AI-0, C-R0.

### AI-2a. Dựng chunk theo chủ đề (thay chunk v1.0)
**Vì sao** (`r0-data-analysis.md` mục 5): chunk v1.0 là 1 khối phẳng mỗi loài; 25% loài cá cảnh bị cắt mất cuối `Comments`; thiếu pH, dH, sinh sản, ghi chú nuôi; khó lọc theo chủ đề.
**Làm:**
- Mỗi loài 4–5 chunk theo chủ đề, gắn `chunkType` **khớp từ điển chủ đề của `QueryNormalizer`** (FishDex): tổng quan; môi trường nước; ăn uống; sinh sản; ghi chú nuôi / phân bố.
- Mỗi chunk lặp lại tên khoa học + tên Anh làm ngữ cảnh (Contextual Retrieval). Toàn bộ tiếng Anh.
- Nguồn: dữ liệu có cấu trúc (`species`, `stocks`, `ecology`, `waterquality`…) cộng văn bản `species.Comments` (không cắt), `ecology.AddRems`, `reproduc.AddInfos`, `diseases.Remark`, `country.Comments`, `species.Remark`. **Bỏ** `morphdat.AddChars`.
- Không cắt chữ giữa chừng; vượt giới hạn token thì tách theo câu.
- Phạm vi 3.751 loài cá cảnh (≈ 15–19 nghìn chunk).
**Xong khi:** test trên parquet mẫu trong `tests/fixtures`; chạy thật trên laptop in thống kê: số chunk theo `chunkType`, độ dài, % loài có từng chủ đề, 0 chunk bị cắt cụt.
**Phụ thuộc:** F6, danh sách `chunkType` chốt trong C-R0.

### AI-2b. Job nạp chunk
**Làm:**
- Đọc đầu ra AI-2a, tính hash cho từng chunk.
- Hỏi FishDex các hash đang có → **chỉ embed loài có thay đổi** → `/embed/batch` (32 câu/lần) → `PUT /api/internal/knowledge/chunks` (≤ 500 chunk/lần).
- Tham số: địa chỉ FishDex, địa chỉ embedding, `--dry-run`, chỉ chạy một số loài. Key qua biến môi trường.
- 503 / lỗi mạng → thử lại có giãn cách; 400 → dừng và in dữ liệu lỗi.
- Bản PROD chạy như container một lần trên VM3, trong mạng private tới FishDex ở VM1.
**Xong khi:** chạy lần 2 không embed lại gì; sửa text 1 loài → chỉ loài đó được gửi; chạy với FishDex local nạp đủ chunk.
**Phụ thuộc:** AI-1, AI-2a, BE-2.

### AI-3. Bộ đánh giá theo tầng + baseline
**Làm:**
- `eval/questions.vi.yaml` ~30 câu theo cấu trúc trong `r0-data-analysis.md` mục 6: tra thuộc tính (8), alias tên Việt (6), so sánh/chồng lấn (5), thiếu dữ liệu — phải từ chối (6), ngoài phạm vi (3), tiếng Việt khó: không dấu, viết tắt (2). **Medaka (Oryzias latipes) ít nhất 3 câu.** Có câu về cá lai / cá cộng đồng. Mỗi câu ghi SpecCode, chủ đề, chunk mong đợi, hành vi mong đợi (trả lời / hỏi lại / từ chối).
- Script đo từng tầng:
  1. Alias: nối đúng SpecCode; báo "mơ hồ" đúng chỗ; không đoán bừa.
  2. Chủ đề: nhận đúng `chunkType`.
  3. Retrieval: hit@k của chunk đúng sau khi lọc.
  4. Câu trả lời (từ R1): đúng, trích dẫn đúng, từ chối đúng.
  5. Hệ thống: tỷ lệ phải dịch bằng LLM, p95, RPM.
- **Baseline** trên cùng bộ câu: (a) câu Việt thô → MiniLM; (b) chuẩn hoá → MiniLM (đã chọn); (c) model đa ngôn ngữ 384 chiều (ví dụ `paraphrase-multilingual-MiniLM-L12-v2`, kiểm tốc độ trên ARM). Nếu (b) không hơn (c) rõ rệt thì báo lại để xem xét quyết định.
- Lưu kết quả mỗi lần chạy để so sánh.
**Phân công:** agent viết script + 5 câu mẫu; **chủ dự án soạn và duyệt đáp án** (cần kiến thức nuôi cá).
**Xong khi:** chạy được tầng 1–3 trên FishDex local; có kết quả lần đầu + bảng baseline.
**Phụ thuộc:** BE-4 (tầng 1–2), BE-5 (tầng 3).

### OPS-1. Bật VM3 (chủ dự án làm, agent hướng dẫn) — cuối R0
Theo checklist trong `ai-release-plan.md` mục VM3:
- Kiểm cấu hình máy (`uname -m`, `nproc`, `free -h`, `lsb_release -a`), ghi IP private vào `Pipeline/OracleVM/VM3/README.md`.
- Cài Docker + Compose, thêm swap.
- Mở cổng 8000/8001 **chỉ cho VM1**, không public.
- Compose ở `Pipeline/OracleVM/VM3` kéo image từ GHCR; model qua volume.
- Đo p95 `/embed` trên VM3 (mục tiêu ≈ 50ms/câu; vượt thì thử ONNX trước).
- Prometheus ở VM2 thu `:8000/metrics`, cảnh báo khi service tắt.
- Tắt VM3 → FishDex phải trả 503.
- Theo dõi chính sách thu hồi máy nhàn rỗi của Oracle Always Free.

---

## R1 — Cổng chất lượng hỏi đáp

### AI-4. Đánh giá end-to-end
**Làm:** chạy tầng 4–5 của bộ đánh giá qua API chat thật (`/aquahome/v1/ai/chat`, chế độ JSON) trên staging: đúng / trích dẫn đúng / từ chối đúng / hỏi lại đúng chỗ; tỷ lệ dịch fallback; p95.
**Chủ dự án:** chốt ngưỡng từng tầng. R1 chỉ đóng khi đạt.
**Phụ thuộc:** AI-3, BE-10.

---

## R3 — Trích trait bù

### AI-5. Job trích trait
**Vì sao:** FishBase không có tính hung dữ, thể tích bể tối thiểu, độ khó nuôi; cờ sống bầy chỉ dương tính ở 2,3% loài cá cảnh.
**Làm:** lấy loài thiếu trait từ báo cáo độ phủ của FishDex; trích từ văn bản (chunk, `AddRems`…) bằng luật + LLM (key qua env); mỗi trait có độ chắc chắn + bằng chứng; dưới ngưỡng thì bỏ. Ghi qua endpoint trait nội bộ ở trạng thái **chờ duyệt**. `--dry-run` xuất CSV để chủ dự án duyệt mẫu.
**Phụ thuộc:** BE-15, DEC-R3.

---

## R4 — Tìm ảnh (song song, khung dựng sớm được)

### AI-6. Service CLIP
FastAPI theo hợp đồng C-R4: `/embed/text`, `/embed/image` (≤ 5MB, jpg/png/webp — kiểm theo nội dung file, không tin đuôi), resize 224, vector 512 chuẩn hoá. Internal key, metric, nạp model 1 lần. Thêm service vào compose VM3 (monorepo) với giới hạn RAM.
**Phụ thuộc:** F6, AI-0, C-R4.

### AI-7. Backfill ~70k ảnh
- Bước 1, trên laptop: lấy danh sách ảnh từ FishDex (ảnh ở bucket R2 `system-image`, URL tạm qua `SystemImage.ObjectKey`), embed theo lô, ghi ra file parquet. Ngắt giữa chừng chạy tiếp được.
- Bước 2, trong mạng private: đọc file, ghi vector vào FishDex theo lô.
- Ảnh lỗi → ghi log, không dừng job.
**Phụ thuộc:** AI-6, BE-17.

### OPS-3. Deploy CLIP lên VM3 (chủ dự án)
Mở cổng 8001 chỉ cho VM1; đo RAM cả 2 service (≈ 3,5/6GB) và p95 embed ảnh trên ARM; metric + cảnh báo RAM > 75%; tắt service → tìm ảnh trả 503.

---

## Thứ tự gợi ý cho AI

1. F6 → AI-0 (ngay)
2. AI-2a (song song với AI-1) → AI-2b khi BE-2 xong
3. AI-3 khi BE-4 / BE-5 xong (chủ dự án soạn câu hỏi song song từ sớm)
4. OPS-1 cuối R0
5. AI-6 dựng sớm khi rảnh → AI-7 khi BE-17 xong → OPS-3
6. AI-4 khi API chat chạy trên staging
7. AI-5 ở R3
