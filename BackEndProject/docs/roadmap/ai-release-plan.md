# Kế hoạch phát hành AI v2.0 — hợp nhất roadmap học tập với cách làm hiện tại

> Nguồn: Notion "AI Learning Roadmap — Unified" (cây Root → Nhánh 1/2/3) và "Epic 2: AI Q&A Service".
> Tài liệu này KHÔNG thay roadmap học; nó thêm lớp **phát hành** (thứ tự, tiêu chí hoàn thành, mobile) lên trên.
> Kế hoạch này sống trong repo (`docs/roadmap/`), không đồng bộ lên Notion theo quyết định của chủ dự án.
> Quyết định đã chốt với chủ dự án: Q1 Nhánh 1 trước · Q2 số đo nước nhập tay trước · Q3 bể 3D tách track riêng · Q4 PWA trước, Capacitor khi có lý do.

## Nguyên tắc

1. **Học vừa đủ, đúng lúc.** Mỗi bản phát hành chỉ kéo đúng các node Root/Nhánh nó cần (cột "Học trước"). Không đọc hết trước khi làm.
2. **Quy tắc trước, LLM sau.** Case có công thức hoặc luật rõ (độc tố, tương thích) chạy bằng rule; LLM chỉ diễn giải kết quả (Explanation Layer, 3.2). Không ép AI vào chỗ rule đã đủ.
3. **Mọi đầu ra AI có độ chắc chắn.** Dữ liệu FishBase nhiều cột null → trả `Score + Explanation + Confidence`, thiếu dữ liệu thì nói thiếu.
4. **Cổng chất lượng theo bản phát hành**, không theo ngày: mỗi bản có tiêu chí hoàn thành đo được (bên dưới).
5. **Ba làn song song, độc lập**: A (Q&A → Analyzer), B (Image Search), C (giao sản phẩm/mobile).

## Các bản phát hành

| # | Bản | Nội dung | Học trước (Notion) | Tiêu chí hoàn thành |
|---|---|---|---|---|
| **R0** | Nền dữ liệu + embedding | Story 2.0 (ingest chunk từ parquet, upsert chạy lại được), 2.1 (FastAPI `/embed` trên VM3, cache 1h, VM3 down → 503) | Root·W (tùy chọn), Root·0, Root·1 | Script re-run không trùng; `/embed` đạt ngân sách; có **bộ câu hỏi chuẩn ~30 câu tiếng Việt** kèm loài đúng để đo retrieval |
| **R1** | AI Q&A | Story 2.2–2.6: pipeline RAG, `ILlmProvider` (Groq/Ollama/Gemini), cache Redis, chat FE + SSE + trích dẫn, quota `aiQaPerDay` | Root·2, Nhánh 1.1 | p95 < 1,5s; Groq 429 → 503; **tỷ lệ trả lời đúng/trích dẫn đúng trên bộ chuẩn** đạt ngưỡng đã chốt; metric RPM lên Grafana |
| **R2** | Số đo nước + cảnh báo độc tố | Entity số đo nước (NH₃/NO₂/NO₃/pH/nhiệt độ, **nhập tay**), luật ngưỡng + xu hướng, nhắc thay nước qua Web Push/Worker, gate theo role/quota | 3.0 (bản nhẹ), Root·0 | Cảnh báo kích hoạt đúng với bộ ca thử; luôn hiện "tham khảo, không thay thế xét nghiệm"; không cần LLM để chạy (LLM chỉ giải thích, tùy chọn) |
| **R3** | Mô phỏng thả thêm cá | Schema trait (EAV/JSONB), trích xuất bù chỗ FishBase thiếu (3.1), `IAquariumAnalyzer` + case tương thích (nhiệt độ/pH chồng lấn, kích thước trưởng thành vs thể tích, hung dữ, nhóm tối thiểu) | 3.0, 3.1, 3.2 | Báo cáo độ phủ trait; mọi kết quả có Confidence; contract test cho analyzer; thêm case mới không sửa foundation |
| **R4** | Image Search (làn B) | Epic 4: CLIP trên VM3, `ClipEmbedding` 512-d, tìm ảnh | Root·1, Nhánh 2.1 | p95 < 700ms; VM3 down → 503. Làm song song, chèn khi rảnh tay, không chặn R1–R3 |
| **R5** | Mobile (làn C) | Cứng hóa PWA: cài đặt, Web Push iOS, trải nghiệm 390px; **cổng quyết định Capacitor** | — | Checklist iPhone 12+ đạt; chỉ đóng gói store nếu PWA thiếu tính năng cụ thể đã ghi nhận |
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
7. **Ghi lại** — cập nhật Notion (trạng thái story) và memory dự án.

## VM3 — khi nào bật, cần cài gì

**Hiện trạng:** VM3 là Ubuntu **ARM (aarch64)**, 6GB RAM (Oracle Always Free), do chủ dự án xác nhận. README ở `Pipeline/OracleVM/VM3/` vẫn ghi *Specs: TBD*, trạng thái đang phát triển, hai thư mục `embedding_service/` và `image_search_service/` còn rỗng. Chưa xác nhận: phiên bản Ubuntu, số OCPU, IP.

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

- Groq free 30 RPM → alert > 25 RPM; 429 → 503, không retry tự động. Fallback Ollama/Gemini qua `ILlmProvider`.
- VM3 (6GB): embedding + CLIP ≈ 3,5GB; không expose public; down → 503, không fallback embedding local.
- pgvector: `SpeciesChunk.Embedding` (384) và `SpeciesMedia.ClipEmbedding` (512) không trộn trong một truy vấn.
- Ngân sách: RAG p95 < 1,5s; Image search p95 < 700ms.
- BE và FE tách commit khi làm feature; mobile 390px là mặc định; mọi chuỗi qua i18n.
- Mọi object key qua `FishDexObjectKeys` / `AquaHomeObjectKeys`; không tự ghép.

## Rủi ro và cách chặn

| Rủi ro | Biện pháp |
|---|---|
| Cảnh báo độc tố sai gây hại cá | Chỉ là tham khảo, ngưỡng lấy từ nguồn nuôi cá tin cậy và ghi nguồn; có bộ ca thử; không tự động hành động |
| FishBase thiếu trait → kết luận sai | Confidence + báo cáo độ phủ; không đưa ra điểm khi dưới ngưỡng dữ liệu |
| LLM bịa | RAG có trích dẫn; bộ câu hỏi chuẩn đo định kỳ; từ chối khi không có ngữ cảnh |
| Quá tải phạm vi | Mỗi bản có tiêu chí hoàn thành; 3D và time-series bị gate |
| Chi phí/quota LLM | Cache Redis theo hash(question + chunk ids); Ollama cho dev |

## Việc mở (cần quyết ở vòng hỏi tiếp theo)

- Ngưỡng chất lượng cụ thể cho R1 (tỷ lệ đúng trên bộ chuẩn) và nguồn ngưỡng độc tố cho R2.
- Role nào được dùng R2/R3/R6, và quota đi theo `RoleQuota` hiện có hay loại mới.
- Số đo nước lưu ở service nào (AquaHome gần nhất) và giữ bao lâu.
