# R0 — Phân tích dữ liệu FishBase v24.07 cho kế hoạch AI

> Nguồn: `D:\Workspace\FishLover\data\fishbase-v24.07` (214 file parquet, đọc bằng DuckDB, chỉ đọc). Chunk v1.0: `FishDex/Pipeline/Preprocessor/output/species_chunks.jsonl`.
> "Cá cảnh" trong tài liệu này = loài có `species.Aquarium` khác `never/rarely` và khác rỗng: **3.751 loài** (3.238 commercial, 359 public aquariums, 90 potential, 62 highly commercial, 2 show aquarium).
> Mọi tỷ lệ là **độ phủ của dữ liệu**, không phải độ đúng của dữ liệu.
> Cập nhật 24/11/2026: thêm quyết định quy câu hỏi về tiếng Anh qua bảng alias (xem mục 4, 5, 6, 7). Kế hoạch phát hành: [`ai-release-plan.md`](ai-release-plan.md).

## Kết luận ngắn

1. **Dữ liệu đủ cho Q&A mô tả loài, không đủ cho phán đoán tương thích hay độc tố theo loài.** FishBase không có tính cách/hung dữ, thể tích bể tối thiểu, độ khó nuôi, và gần như không có ngưỡng nitơ theo loài.
2. **R2 (độc tố) phải dùng ngưỡng ngoài FishBase**, có nguồn trích dẫn, áp dụng chung. FishBase không thay thế được.
3. **R3 (tương thích) chỉ chạy được ở mức "chồng lấn thông số nước + kích thước + ăn mồi" cho khoảng một phần tư loài cá cảnh**; còn lại phải trả "chưa đủ dữ liệu", đúng nguyên tắc Confidence của kế hoạch.
4. **R1 (Q&A) nên dựng lại chunk**, không dùng nguyên chunk v1.0: 25% loài cá cảnh bị cắt mất phần cuối `Comments`, và thiếu pH/dH/sinh sản; chunk một khối phẳng nên khó lọc theo chủ đề.
5. **Tiếng Việt là rủi ro lớn nhất của R1**: tên Việt chỉ có ở 6,6% loài cá cảnh; MiniLM là model tiếng Anh. **Hướng đã chọn:** quy câu hỏi về tiếng Anh bằng bảng alias + từ điển chủ đề (luật trước, LLM dịch chỉ là fallback), chunk giữ tiếng Anh. Rủi ro còn lại chuyển sang chất lượng bảng alias, và ngân sách RPM/độ trễ nếu phải dịch bằng LLM.

## 1. Phạm vi và nội dung có sẵn

| Hạng mục | Số liệu |
|---|---|
| Tổng loài (`species`) | 35.731 (cờ nước ngọt 17.910, mặn 18.491, lợ 3.184 — các cờ chồng lấn nhau) |
| Loài cá cảnh | 3.751 |
| Cá cảnh có `Comments` | 3.454 (92,1%); trung bình 303 ký tự, p90 586 |
| Cá cảnh có tên tiếng Anh (`FBname`) | 78,1% |
| Cá cảnh có chiều dài tối đa | 99,7% |
| Cá cảnh có ảnh ưu tiên | 89,9% |
| Cá cảnh có tuổi thọ nuôi nhốt | 7,6% (`LongevityWild`) |

Tổng văn bản mô tả rất nhỏ: `species.Comments` toàn bảng ≈ 5MB, riêng cá cảnh ≈ 1MB. Bản thân `Comments` không đủ để RAG trả lời tốt; chunk phải được dựng từ cả dữ liệu có cấu trúc.

## 2. Thông số nước và sinh học (nền cho R2/R3)

Trên 3.751 loài cá cảnh (lấy hợp các `stock` của mỗi loài):

| Thông số | Số loài có | Tỷ lệ |
|---|---|---|
| Khoảng nhiệt độ (`TempMin`+`TempMax`) | 1.868 | 49,8% |
| Nhiệt độ ưa thích (`TempPreferred`) | 124 | 3,3% |
| Khoảng pH | 995 | 26,5% |
| Khoảng dH | 606 | 16,2% |
| Đủ cả nhiệt độ **và** pH | 980 | 26,1% |
| Kiểu ăn (`FeedingType`) | 2.083 | 55,5% |
| Cờ sống bầy dương tính (`Schooling`/`Shoaling`/`Solitary` = -1) | 85 | 2,3% |
| Kiểu sinh sản (`ReproMode`) | 2.492 | 66,4% |
| Chăm sóc con (`ParentalCare`) | 1.423 | 37,9% |
| Có dữ liệu thức ăn chi tiết (`diet`) | 718 | 19,1% |
| Có dữ liệu bị săn (`predats`) | 492 | 13,1% |
| Có dữ liệu bệnh (`diseases`) | 476 | 12,7% |

Lưu ý khi dùng:
- **Cờ boolean dạng Access (-1 = có, 0 = không)**: 0 gộp chung "không" và "chưa ghi nhận" (toàn bảng chỉ 142 dòng `Schooling = -1` so với 12.424 dòng `0`). Không được suy ra "không sống bầy" từ giá trị 0.
- **Chất lượng khoảng giá trị**: không có khoảng ngược; 2 pH ngoại lai. Khoảng nhiệt độ trung vị 4°C, nhưng 85 loài có biên >15°C và 29 loài >20°C (ví dụ cá vàng `0–41°C`, lấy từ biên phân bố tự nhiên) — vô nghĩa cho luật. Đề xuất: loại khoảng >15°C khỏi luật chồng lấn và đánh dấu "biên rộng".
- **Cá cảnh gồm cả loài nước mặn**; pH/dH chỉ có ý nghĩa ở nước ngọt. Luật phải tách theo `WaterType`.
- 148 loài có nhiều `stock` với thông số khác nhau; cần quy tắc gộp (hợp rộng nhất hay stock đầu tiên) — hiện FishDex dùng stock đầu tiên.

**Không có ở bất kỳ bảng nào (quét tên cột của cả 214 bảng):** tính cách/hung dữ, thể tích/kích thước bể tối thiểu, độ khó nuôi. (`DIFFICULTY` trong `cigua` nói về độc ciguatera, không phải nuôi.)

### Ví dụ thật: "cá lia thia nuôi chung với neon được không?"

| Loài | Nhiệt độ | pH | dH | Dài | Kiểu ăn | Tên Việt |
|---|---|---|---|---|---|---|
| Betta splendens | 24–30 | 6–8 | 5–19 | 6,5 cm | không có | Cá lia thia |
| Paracheirodon innesi (neon) | 20–26 | 5–7 | 1–2 | 2,2 cm | variable | không có |

Chồng lấn: nhiệt độ 24–26°C (được), pH 6–7 (được), **dH không chồng lấn** (5–19 so với 1–2). Dữ liệu chỉ cho phép kết luận "thông số nước không khớp ở độ cứng", **không** nói được betta có cắn vây hay không (không có dữ liệu tính cách) và không đánh dấu neon là cá sống bầy (cờ = 0). Câu trả lời đúng của hệ thống: trả phần chồng lấn có số liệu + nói rõ phần nào thiếu dữ liệu.

## 3. Độc tố (R2)

- `cl50` (LC50): 1.434 dòng nhưng chỉ **95 loài**, phần lớn là hóa chất công nghiệp/thuốc trừ sâu (hydrocarbon 539, organophosphate 337…).
- **Amoniac:** ~41 dòng (un-ionized ammonia 38 dòng/11 loài, trung vị 0,91 mg/L ở 24–96 giờ). **Nitrit, nitrat: 0 dòng.**
- `waterquality`: 6 dòng, ~4 dòng dùng được, chuỗi dạng `0.02mg/l`, là ngưỡng cho nuôi trồng thủy sản của vài loài — không tổng quát hóa cho bể cảnh.
- `aquarium` (7.023 dòng) và `aquamaint` (1.425 dòng) **không chứa thông số nuôi**: lần lượt là danh sách cá ở các thủy cung và đường link website ngoài.

→ Xác nhận quyết định của kế hoạch: R2 là bộ luật ngưỡng chung (NH₃, NO₂, NO₃, pH, nhiệt độ) lấy từ nguồn nuôi cá có trích dẫn, kèm cảnh báo tham khảo. Có thể dùng `cl50` làm **tài liệu tham chiếu hiển thị** cho vài loài có số liệu, không làm ngưỡng cảnh báo tự động.

## 4. Tên tiếng Việt (rủi ro của R1)

- Toàn FishBase: 1.448 tên Việt cho 1.100 loài.
- Cá cảnh có tên Việt: **249/3.751 (6,6%)**; tiếng Anh 82,0%, Quan Thoại 93,3%, Đức 46,7%.
- Trong 8 loài phổ biến thử, chỉ 3 loài có tên Việt; cá vàng (Carassius auratus) có tên Việt là "Cá Diếc" (sai thông dụng), không có "cá vàng".
- Chức năng đóng góp tên Việt của cộng đồng (`CommunityCommonName`, đã có trong FishDex) là **nguồn bổ sung duy nhất** và cần được dùng làm lớp alias cho truy vấn.

Hệ quả: truy vấn "cá bảy màu", "cá lia thia", "cá vàng" phải đi qua một bước chuẩn hóa tên (alias → SpecCode) **trước** khi tìm vector, vì embedding tiếng Anh sẽ không nối được tên Việt với loài.

**Hướng xử lý đã chọn (24/11/2026):** bảng `SpeciesAlias` gom nhiều nguồn tên về một SpecCode:

| Nguồn alias | Quy mô (theo dữ liệu hiện có) | Ghi chú |
|---|---|---|
| Tên Việt FishBase | 1.448 tên / 1.100 loài (249 loài cá cảnh) | Thiếu nhiều tên thông dụng (ví dụ "cá vàng") |
| Tên cộng đồng đã duyệt (`CommunityCommonName`) | tăng dần | Nguồn bổ sung duy nhất cho tên Việt thật |
| Tên Anh `FBname` | 78,1% loài cá cảnh | Có thể khớp các từ mượn người chơi hay gõ (betta, guppy, neon) — **giả thuyết, cần đo bằng bộ test alias** |
| Tên khoa học | 100% | Dùng cho người gõ tên Latin |
| Danh sách seed tên chơi cá | do chủ dự án duyệt | Bù chỗ FishBase thiếu; cần chủ sở hữu rõ ràng |

Các điểm phải xử lý: bỏ dấu để so khớp (câu không dấu như "ca lia thia"), tên mơ hồ (một tên khớp nhiều loài → hỏi lại), và đo độ chính xác alias như một tầng riêng (mục 6).

## 5. Chunk v1.0 và đề xuất dựng lại

Hiện trạng (`species_chunks.jsonl`): 8.883 chunk, **một chunk mỗi loài** (`chunkIndex` luôn 0), trung vị 482 ký tự, tổng 4,43MB. Phủ 100% loài cá cảnh (+5.132 loài khác). Trường: Species, Family/Order/Class, Water type, Position in water, Size, Aquarium trade, Common name (chỉ 6.337), Temperature (chỉ 2.250), Feeding, Dangerous, Lifespan in captivity, Notes.

Vấn đề:
- **Cắt cụt:** 870/3.454 loài cá cảnh (25,2%) có `Notes` ngắn hơn `Comments` gốc (mất trung bình 225 ký tự, tổng ≈ 0,2MB).
- **Thiếu:** pH, dH (0/3.751 trong chunk), sinh sản (45), sống bầy (270 lần nhắc), thức ăn chi tiết, `AddRems`/`AddInfos` có ghi chú nuôi. Tên Việt cũng không có trong chunk, nhưng **không còn cần cho retrieval**: câu hỏi được quy về tiếng Anh trước khi tìm, tên Việt chỉ dùng ở bảng alias và khi hiển thị.
- Chunk dạng "danh sách thuộc tính" ngắn, khó khớp với câu hỏi tự nhiên.

Đề xuất thiết kế chunk (chờ quyết ở vòng `grilling`):
- **Nhiều chunk theo chủ đề mỗi loài** (tổng quan; môi trường nước; ăn uống; sinh sản; ghi chú nuôi/phân bố) thay vì một chunk phẳng, mỗi chunk gắn `chunkType`. Mỗi chunk lặp lại tên khoa học + tên Anh làm ngữ cảnh — đúng tinh thần Contextual Retrieval trong roadmap. **Toàn bộ chunk bằng tiếng Anh.**
- **`chunkType` khớp với từ điển chủ đề của `QueryNormalizer`**: câu hỏi về "ăn gì" chỉ tìm trong chunk ăn uống của đúng loài đã nhận diện (lọc cứng theo SpecCode + chunkType), vector chỉ xếp hạng trong tập nhỏ đó. Đây là cách giảm phụ thuộc vào chất lượng embedding.
- Nguồn văn bản bổ sung có giá trị: `ecology.AddRems` (8.972 loài), `reproduc.AddInfos` (7.083), `diseases.Remark`, `country.Comments` (phân bố/biotope, 33.202 loài, ngắn), `species.Remark`. **Bỏ `morphdat.AddChars`** (13,5MB nhưng là ghi chú phân loại, ít giá trị cho người nuôi).
- Không chunk các loài `never/rarely` ở đợt đầu: giữ phạm vi 3.751 loài (khoảng 1MB văn bản gốc) để đo chất lượng nhanh, mở rộng sau.
- Ước lượng quy mô: ~3.751 loài × 4–5 chunk ≈ 15–19 nghìn chunk × vector 384 chiều — rất nhỏ so với dung lượng pgvector và VM3.

## 6. Bộ câu hỏi chuẩn (bản nháp cấu trúc, chưa viết nội dung)

Phân loại để đo chất lượng R1, tỷ lệ gợi ý trên ~30 câu:

| Loại | Số câu | Ví dụ | Kỳ vọng |
|---|---|---|---|
| Tra cứu thuộc tính có dữ liệu | 8 | "Cá neon sống ở nhiệt độ nào?" | Trả số đúng + trích dẫn |
| Alias tên Việt | 6 | "Cá bảy màu ăn gì?" | Nối đúng SpecCode |
| So sánh/chồng lấn thông số | 5 | "Betta và neon chung bể được không?" | Phần chồng lấn + nêu rõ thiếu dữ liệu |
| Câu thiếu dữ liệu (phải từ chối) | 6 | "Betta có cắn vây neon không?" | Nói không có dữ liệu, không bịa |
| Ngoài phạm vi | 3 | "Giá cá koi hôm nay?" | Từ chối lịch sự |
| Tiếng Việt khó (không dấu, viết tắt) | 2 | "ca lia thia" | Vẫn khớp |

Loài chủ đề Medaka (Oryzias latipes) nên có ít nhất 3 câu, vì là loài trọng tâm của sản phẩm (chunk hiện chỉ có `Temperature 18–24°C`, thiếu pH 7–8 và ghi chú nước muối loãng).

**Đo theo từng tầng** (để biết lỗi nằm ở đâu, thay vì chỉ có một điểm cuối):

| Tầng | Đo gì | Bộ dữ liệu |
|---|---|---|
| 1. Alias | Tỷ lệ nối đúng SpecCode; tỷ lệ trả "mơ hồ" đúng chỗ; tỷ lệ không-khớp được phát hiện (không đoán bừa) | Cặp (câu Việt → SpecCode kỳ vọng), gồm câu không dấu |
| 2. Chủ đề | Tỷ lệ nhận đúng chủ đề / chunkType | Cùng bộ câu hỏi |
| 3. Retrieval | hit@k của chunk đúng sau khi lọc SpecCode + chunkType | Chunk kỳ vọng cho mỗi câu |
| 4. Câu trả lời | Đúng, có trích dẫn đúng, từ chối đúng khi thiếu dữ liệu | Chấm tay lần đầu, sau đó bán tự động |
| 5. Hệ thống | Tỷ lệ phải gọi LLM dịch fallback; độ trễ p95; RPM | Log khi chạy bộ câu hỏi |

**Baseline so sánh ở R0** (không bắt buộc áp dụng): (a) câu Việt thô → MiniLM, (b) chuẩn hóa → MiniLM (đã chọn), (c) model đa ngôn ngữ cùng 384 chiều. Chạy trên cùng bộ câu hỏi để có số liệu cho quyết định.

## 7. Tác động lên kế hoạch phát hành

| Bản | Điều chỉnh đề xuất |
|---|---|
| R0 | Thêm `SpeciesAlias` + `QueryNormalizer` (luật) vào phạm vi; dựng lại chunk tiếng Anh theo chủ đề (`chunkType`); chốt phạm vi 3.751 loài trước; đo theo tầng + baseline |
| R1 | Pipeline chuẩn hóa → lọc SpecCode/chunkType → vector → LLM sinh tiếng Việt; LLM dịch chỉ là fallback có cache; tiêu chí thêm: từ chối đúng nhóm "thiếu dữ liệu", đo tỷ lệ fallback và ngân sách RPM |
| R2 | Giữ nguyên hướng luật ngưỡng ngoài; **không** dùng FishBase làm ngưỡng độc; có thể hiển thị `cl50` như tham khảo |
| R3 | Giới hạn mô phỏng ở nhiệt độ/pH/dH + kích thước + ăn mồi; thêm ngưỡng "đủ dữ liệu" (khoảng 26% loài cá cảnh đủ nhiệt độ và pH); tính cách và sống bầy cần nguồn bổ sung (đóng góp cộng đồng hoặc trích xuất có kiểm duyệt) — đây là phạm vi của 3.0/3.1 |
| Dữ liệu | Quy tắc gộp nhiều `stock`; loại biên nhiệt độ >15°C khỏi luật; tách luật nước ngọt/mặn |

## 8. Điều chưa kiểm tra

- Chất lượng thực tế của MiniLM trên câu tiếng Anh đã chuẩn hóa so với câu Việt thô (chưa chạy; cần Root·1 và máy có `sentence-transformers`).
- Tỷ lệ câu hỏi thật của người dùng mà luật (alias + từ điển chủ đề) xử lý được, tức tỷ lệ phải dùng LLM dịch. Quyết định kiến trúc phụ thuộc con số này; cần log câu hỏi thật hoặc bộ câu hỏi chuẩn.
- Hiệu quả của tên Anh `FBname` làm alias cho từ mượn (betta, guppy, neon) — mới là giả thuyết.
- Độ chính xác của dữ liệu (chỉ đo độ phủ); chưa đối chiếu với nguồn nuôi cá bên ngoài.
- Bảng đa-stock: chưa đo mức khác biệt giữa các stock của cùng một loài.
- Chưa đo trùng lặp giữa `SpeciesSnapshot` Community (loài lai) và các bảng này — loài lai không có trong FishBase nên không có dữ liệu ở đây.
- Các script phân tích nằm ở thư mục scratchpad của phiên, chưa đưa vào repo.
