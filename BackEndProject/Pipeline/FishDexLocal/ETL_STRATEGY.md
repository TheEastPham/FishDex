# FishDex — Chiến lược ETL (Parquet → PostgreSQL)

## Mục tiêu

Load dữ liệu FishBase từ parquet vào PostgreSQL local, lọc **cá nước ngọt phổ biến trong thủy sinh**,
sao cho data này có thể dùng thẳng cho PROD sau khi verify.

---

## Trạng thái hiện tại

| Hạng mục | Trạng thái |
|---|---|
| PostgreSQL Docker Compose | ✅ `docker compose up -d` trong thư mục này |
| EF Core migrations (`InitialCreate` + `AddCommonName`) | ✅ Chạy `dotnet ef database update` để apply |
| pgvector extension | ✅ `init.sql` enable tự động khi container khởi tạo lần đầu |
| ETL scripts (Python + polars + psycopg2) | ✅ Đã có ở `etl/` |
| Parquet data files | ⏳ **Chờ bổ sung vào `parquetData/`** |

---

## Cách chạy step by step

```bash
# ── Setup môi trường ────────────────────────────────────────────────────
cd BackEndProject/Pipeline/FishDexLocal

# 1. Khởi động PostgreSQL container
docker compose up -d

# 2. Apply EF Core migrations (chạy từ BackEndProject/)
cd ../..
dotnet ef database update --project FishDex/FishDex.EFCore --startup-project FishDex/FishDex.API
cd Pipeline/FishDexLocal

# 3. Bỏ file parquet vào parquetData/  (xem danh sách bên dưới)

# 4. Tạo virtualenv & cài dependencies
python -m venv .venv
.venv\Scripts\activate           # Windows PowerShell
# source .venv/bin/activate      # macOS/Linux
pip install -r requirements.txt

# ── ETL pipeline ────────────────────────────────────────────────────────

# Step 0: kiểm tra giá trị Aquarium thực tế trong parquet trước khi load
python inspect.py
#   → in ra value_counts của Aquarium và AquariumFishII
#   → cho biết số loài sẽ pass filter

# (tuỳ chỉnh nếu cần)
# Edit etl/config.py:
#   AQUARIUM_VALUES   = {"highly commercial", "commercial", ...}
#   INCLUDE_BRACKISH  = False

# Step 1: dry-run xác nhận spec_codes
python -m etl.run --dry-run

# Step 2: chạy full ETL
python -m etl.run

# (hoặc chạy theo từng bước nhỏ — debug từng bảng)
python -m etl.run --only 1      # chỉ load Families
python -m etl.run --steps 1,2,3 # Families + Genuses + Species
python -m etl.run --from 5      # từ Ecology trở đi

# Step 3: reset sequences sau ETL (cần thiết vì insert explicit PK)
docker exec -i fishdex-postgres psql -U fishdex -d fishdex < post_etl.sql
```

> **EF tools version warning**: Local đang dùng `dotnet-ef 8.x`, nên update:
> `dotnet tool update --global dotnet-ef`

---

## Parquet files cần có trong `parquetData/`

| File | Bảng đích | Bắt buộc? |
|---|---|---|
| `families.parquet` | Families | ✅ |
| `genera.parquet` | Genuses | ✅ |
| `species.parquet` | Species | ✅ **(filter at this step)** |
| `stocks.parquet` | Stocks + Conservation + Environment + ExternalRef + Metadata | ✅ |
| `ecology.parquet` | Ecologies + HabitatZones + FeedingAndDiets + Associations + Substrates + SpecialHabitats + CircadianBehaviors | ✅ |
| `morphdat.parquet` | MorphData + Teeth + Pigmentation + Fins + Meristics + Metrics | ⚠️ Optional |
| `ecosystemref.parquet` | EcosystemRefs (load TOÀN BỘ, không filter) | ⚠️ Optional |
| `ecosystem.parquet` | Ecosystems (junction, filter spec_codes) | ⚠️ Optional |
| `occurrence.parquet` | Occurrences | ⚠️ Optional — file rất lớn |
| `comnames.parquet` | CommonNames | ✅ |
| `pictures.parquet` | SystemImages | ⚠️ Optional |

> Nếu thiếu file optional, loader sẽ in `SKIP` và tiếp tục — không crash.

---

## Bộ lọc — Freshwater Aquarium Fish

### Field lọc chính trong `species.parquet`

| Column | Mô tả | Filter logic |
|---|---|---|
| `Fresh` | Cá nước ngọt (0/1) | `== 1` |
| `Brack` | Cá nước lợ (0/1) | optional via `INCLUDE_BRACKISH` |
| `Aquarium` | Mức độ phổ biến thủy sinh (text \| null) | xem giá trị bên dưới |
| `AquariumFishII` | Trạng thái thứ 2 | refinement (không filter chính) |

### Giá trị `Aquarium` field

FishBase thường dùng các giá trị:

| Giá trị | Ý nghĩa | Lấy vào? |
|---|---|---|
| `'highly commercial'` | Rất phổ biến trong thủy sinh | ✅ |
| `'commercial'` | Phổ biến | ✅ |
| `'of minor importance'` | Ít phổ biến hơn | ⚠️ Tùy quyết định trong `config.py` |
| `'never/rarely'` | Không phải cá thủy sinh | ❌ Bỏ |
| `'potential'` | Tiềm năng | ❌ Bỏ |
| `null` / empty | Không phải cá thủy sinh | ❌ Bỏ |

> **Verify**: chạy `python inspect.py` để xem value_counts thực tế trong parquet bạn đang dùng.

**Ước tính**: Freshwater + popular aquarium → khoảng **2,000–3,500 loài** (so với tổng ~35,000 loài FishBase).

---

## Thứ tự load (FK dependency order)

```
1.  families      → "Families"          (không có FK)
2.  genera        → "Genuses"           (FK: FamCode → Families)
3.  species       → "Species"           (FK: GenCode → Genuses, FamCode → Families)
                                         *** LỌC spec_codes TẠI ĐÂY ***
4.  stocks        → "Stocks" + StockConservations + StockEnvironments
                  + StockExternalRefs + StockMetadatas
                                         (FK: SpecCode → Species)
5.  ecology       → "Ecologies" + HabitatZones + FeedingAndDiets + Associations
                  + Substrates + SpecialHabitats + CircadianBehaviors
                                         (FK: SpecCode → Species)
6.  morphdat      → "MorphData" + MorphTeeth + MorphPigmentations + MorphFins
                  + MorphMeristics + MorphMetrics
                                         (FK: StockCode → Stocks)
7.  ecosystemref  → "EcosystemRefs"      (load TOÀN BỘ, không filter)
8.  ecosystem     → "Ecosystems"         (FK: E_CODE → EcosystemRefs, SpecCode → Species)
9.  occurrence    → "Occurrences"        (FK: SpecCode → Species)
10. comnames      → "CommonNames"        (FK: SpecCode → Species)
11. images        → "SystemImages"       (FK: SpecCode → Species) — optional
```

---

## Column mapping (parquet → entity)

### `species.parquet` → `Species`

| Parquet column | Entity property | Ghi chú |
|---|---|---|
| `SpecCode` | `SpecCode` | |
| `Genus` + `" "` + `Species` | `SpeciesName` | Ghép trong loader |
| `FamCode` | `FamCode` | |
| `GenCode` | `GenusCode` | |
| `Fresh`/`Brack`/`Saltwater` | `WaterType` (enum) | Fresh→1, Brack→3, Salt→2 |
| `BodyShapeI` | `BodyShapeI` | |
| `LongevityWild` | `LongevityWild` | |
| `LengthFemale` | `LengthFemale` | |
| `PicPreferredNameM` | `PicPreferredNameM` | |
| `PicPreferredNameF` | `PicPreferredNameF` | |
| `DemersPelag` | `DemersPelag` | |
| `MaxLengthRef` | `MaxLengthRef` | |
| `Source`, `Author`, `AuthorRef`, `Remark`, `TaxIssue` | giống tên | |
| `Length`, `Weight`, `Vulnerability`, `VulnerabilityClimate` | giống tên | |
| `AirBreathing`, `LifeCycle`, `Dangerous`, `Comments` | giống tên | |

### `comnames.parquet` → `CommonNames`

| Parquet column | Entity property | Ghi chú |
|---|---|---|
| `autoctr` | `AutoCtr` | PK |
| `SpecCode` | `SpecCode` | FK → Species |
| `StockCode` | `StockCode` | nullable |
| `ComName` | `ComName` | tên cần tìm kiếm |
| `Transliteration` | `Transliteration` | romanized |
| `C_Code` | `CountryCode` | |
| `Language` | `Language` | "English", "Vietnamese", ... |
| `NameType` | `NameType` | "Vernacular" / "Trade" |
| `PreferredName` | `IsPreferred` | 0/1 → bool |
| `Rank` | `Rank` | thứ tự ưu tiên |
| `Remarks` | `Remarks` | |

### `families.parquet` → `Families`

| Parquet column | Entity property | Ghi chú |
|---|---|---|
| (generated UUID) | `Id` (Guid) | `uuid.uuid4()` trong loader |
| `FamCode` | `FamCode` | |
| `Family` | `Name` | |
| `CommonName` / `FBname` | `CommonName` | fallback |
| `BodyShapeI` | `BodyShapeI` | |
| `SwimMode` | `SwimMode` | |
| `ReprGuild` | `ReproductiveGuild` | |

### `genera.parquet` → `Genuses`

| Parquet column | Entity property | Ghi chú |
|---|---|---|
| `GenCode` | `GenusCode` | PK |
| `FamCode` | `FamId` | **lookup Family.Id** từ DB sau khi load step 1 |
| `Genus` | `GenusName` | |

> Chi tiết các bảng còn lại (Stocks, Ecology, Morph, Ecosystem...) — xem trực tiếp source `etl/loaders/*.py`,
> các SQL INSERT đã liệt kê đầy đủ tên cột.

---

## Cấu trúc thư mục ETL

```
Pipeline/FishDexLocal/
├── parquetData/                ← bỏ file .parquet ở đây
├── etl/
│   ├── __init__.py
│   ├── config.py               ← DB_URL, filter config
│   ├── db.py                   ← connect + upsert helpers + type coercion
│   ├── filter.py               ← compute_spec_codes()
│   ├── loaders/
│   │   ├── __init__.py
│   │   ├── families.py
│   │   ├── genera.py
│   │   ├── species.py
│   │   ├── stocks.py           ← Stock + 4 sub-tables từ 1 parquet
│   │   ├── ecology.py          ← Ecology + 6 sub-tables từ 1 parquet
│   │   ├── morph.py            ← MorphData + 5 sub-tables từ 1 parquet
│   │   ├── ecosystem.py        ← EcosystemRef + Ecosystem junction
│   │   ├── occurrence.py
│   │   ├── common_names.py
│   │   └── images.py
│   └── run.py                  ← entry point, gọi theo FK order
├── inspect.py                  ← step 0: check Aquarium values
├── post_etl.sql                ← reset sequences sau khi insert explicit PK
├── requirements.txt
├── init.sql                    ← pgvector extension
├── ETL_STRATEGY.md             ← file này
└── docker-compose.yml
```

---

## Nguyên tắc thiết kế script

| Nguyên tắc | Cách triển khai |
|---|---|
| **Idempotent** | UPSERT (`ON CONFLICT DO UPDATE`) cho mọi bảng có natural PK. Bảng PK auto-gen (Occurrence, SystemImage) dùng `DELETE WHERE SpecCode IN spec_codes` rồi INSERT lại. |
| **Defensive column access** | `row.get("col")` — không crash khi parquet thiếu cột. |
| **Batch insert** | `executemany` batch 500 rows; fail batch → fallback row-by-row, log skipped. |
| **Transaction per table** | Commit sau mỗi bảng — fail bảng nào chỉ rollback bảng đó. |
| **Step-by-step** | `--only N`, `--steps N,M`, `--from N` cho phép chạy lại từng phần. |
| **Logging rõ ràng** | `[Species] Inserted/Updated: 2341 | Skipped: 12` cho mỗi bảng. |

---

## Prod deployment

Sau khi verify data trên local:

```bash
# Dump data-only (PROD sẽ tự chạy migrations để có schema)
pg_dump \
  --host=localhost --port=5433 \
  --username=fishdex --dbname=fishdex \
  --data-only --no-owner --no-acl \
  --file=fishdex_seed_data.sql

# Restore lên PROD (sau khi PROD đã chạy `dotnet ef database update`)
psql -h <prod-host> -U fishdex -d fishdex -f fishdex_seed_data.sql
psql -h <prod-host> -U fishdex -d fishdex -f post_etl.sql
```

**Hoặc** chạy ETL script thẳng lên PROD bằng cách set env var:
```bash
DB_URL=postgresql://user:pass@prod-host:5432/fishdex python -m etl.run
```

---

## Checklist khi chạy

- [ ] `docker compose up -d` → PostgreSQL running trên port 5433
- [ ] `dotnet ef database update` → schema đã apply
- [ ] Bỏ tất cả parquet files vào `parquetData/`
- [ ] `pip install -r requirements.txt`
- [ ] `python inspect.py` → xác nhận giá trị `Aquarium` thực tế
- [ ] Edit `etl/config.py` nếu cần (filter values, brackish, DB url)
- [ ] `python -m etl.run --dry-run` → xem số spec_codes match
- [ ] `python -m etl.run` → full ETL
- [ ] `psql ... < post_etl.sql` → reset sequences
- [ ] Verify trên DB: `SELECT COUNT(*) FROM "Species";` …
