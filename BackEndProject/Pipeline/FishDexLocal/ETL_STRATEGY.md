# FishDex — Chiến lược ETL (Parquet → PostgreSQL)

## Mục tiêu

Load dữ liệu FishBase từ parquet vào PostgreSQL local, lọc **cá nước ngọt phổ biến trong thủy sinh**,
sao cho data này có thể dùng thẳng cho PROD sau khi verify.

---

## Trạng thái hiện tại

| Hạng mục | Trạng thái |
|---|---|
| PostgreSQL Docker Compose | ✅ Sẵn sàng (`docker compose up -d` trong thư mục này) |
| EF Core migration `InitialCreate` | ✅ Đã tạo — chạy `dotnet ef database update` để apply |
| pgvector extension | ✅ `init.sql` sẽ enable tự động khi container khởi tạo lần đầu |
| Parquet data files | ⏳ **Chờ bổ sung vào `parquetData/`** |
| ETL script | ⏳ Chờ confirm data source → viết sau |

---

## Bước 0 — Cần làm trước khi ETL

```bash
# 1. Khởi động PostgreSQL
cd BackEndProject/Pipeline/FishDexLocal
docker compose up -d

# 2. Apply migration (chạy từ BackEndProject/)
cd ../..
dotnet ef database update --project FishDex/FishDex.EFCore --startup-project FishDex/FishDex.API

# 3. Bỏ parquet files vào:
#    BackEndProject/Pipeline/FishDexLocal/parquetData/
#    Các file cần thiết: species.parquet, stocks.parquet, ecology.parquet,
#    habitat.parquet, feeding.parquet, morphdat.parquet,
#    occurrence.parquet, ecosystemref.parquet, ecosystem.parquet,
#    families.parquet, genera.parquet, speciesimages.parquet (optional)
```

> **EF tools version warning**: Local machine đang dùng `dotnet-ef 8.x`, nên update:
> `dotnet tool update --global dotnet-ef`

---

## Bộ lọc — Freshwater Aquarium Fish

### Field lọc chính trong `species.csv`

| Column | Mô tả | Filter logic |
|---|---|---|
| `Fresh` | Cá nước ngọt (0/1) | `== 1` |
| `Aquarium` | Mức độ phổ biến thủy sinh (text \| null) | Xem giá trị bên dưới |
| `AquariumFishII` | Trạng thái thứ 2 (text \| null) | Optional refinement |

### Giá trị `Aquarium` field (cần verify khi có data)

FishBase thường dùng các giá trị:

| Giá trị | Ý nghĩa | Lấy vào? |
|---|---|---|
| `'highly commercial'` | Rất phổ biến trong thủy sinh | ✅ |
| `'commercial'` | Phổ biến | ✅ |
| `'of minor importance'` | Ít phổ biến hơn | ⚠️ Tùy quyết định |
| `'potential'` | Tiềm năng | ❌ Bỏ |
| `null` / empty | Không phải cá thủy sinh | ❌ Bỏ |

> **TODO**: Khi có parquet, chạy `species['Aquarium'].value_counts()` để xem các giá trị thực tế,
> sau đó chốt filter. Có thể giá trị là `1`/`0`, hoặc text khác.

### Filter query tổng hợp (pseudocode)

```python
aquarium_species = species[
    (species['Fresh'] == 1) &
    (species['Aquarium'].isin(['highly commercial', 'commercial']))
]
spec_codes = set(aquarium_species['SpecCode'])
# Dùng spec_codes này để filter TẤT CẢ bảng còn lại
```

**Ước tính**: Freshwater + popular aquarium → khoảng 2,000–3,500 loài
(so với tổng ~35,000 loài trong FishBase).

---

## Thứ tự load (FK dependency order)

Phải load theo thứ tự này để tránh FK violation:

```
1.  families          → Family        (không có FK)
2.  genera            → Genus         (FK: FamCode → families)
3.  species           → Species       (FK: GenCode → genera, FamCode → families)
                                       *** LỌC spec_codes TẠI ĐÂY ***
4.  stocks            → Stock         (FK: SpecCode → species)
5.  stock_conservation→ StockConservation (FK: StockCode → stocks)
6.  stock_environment → StockEnvironment  (FK: StockCode → stocks)
7.  stock_externalref → StockExternalRef  (FK: StockCode → stocks)
8.  ecology           → Ecology       (FK: SpecCode → species)
9.  habitat           → HabitatZone   (FK: EcologyId → ecology)
10. feeding           → FeedingAndDiet(FK: EcologyId → ecology)
11. morphdat          → MorphData     (FK: StockCode → stocks)
12. ecosystemref      → EcosystemRef  (không có FK species — load độc lập)
13. ecosystem         → Ecosystem     (FK: E_CODE → ecosystemref, SpecCode → species)
14. occurrence        → Occurrence    (FK: SpecCode → species)
15. speciesimages     → SystemImage   (FK: SpecCode → species)
```

---

## Column mapping quan trọng (parquet → entity)

### `species.csv` → `Species` entity

| Parquet column | Entity property | Ghi chú |
|---|---|---|
| `SpecCode` | `SpecCode` | |
| `Genus` + `" "` + `Species` | `SpeciesName` | Ghép tên đầy đủ |
| `FamCode` | `FamCode` | |
| `GenCode` | `GenusCode` | |
| `Fresh`=1, `Brack`=1, `Saltwater`=1 | `WaterType` (enum) | Ưu tiên Fresh > Brack > Salt |
| `BodyShapeI` | `BodyShapeI` | |
| `LongevityWild` | `LongevityWild` | |
| `LengthFemale` | `LengthFemale` | |
| `PicPreferredNameM` | `PicPreferredNameM` | |
| `PicPreferredNameF` | `PicPreferredNameF` | |
| `DemersPelag` | `DemersPelag` | |
| `MaxLengthRef` | `MaxLengthRef` | |

### `families.csv` → `Family` entity

| Parquet column | Entity property |
|---|---|
| `FamCode` | `FamCode` |
| `Family` | `Name` |
| `CommonName` | `CommonName` |
| `BodyShapeI` | `BodyShapeI` |
| `SwimMode` | `SwimMode` |
| `ReprGuild` | `ReproductiveGuild` |
> `Id` (Guid) → **tạo mới** `Guid.NewGuid()` trong ETL script

### `genera.csv` → `Genus` entity

| Parquet column | Entity property |
|---|---|
| `GenCode` | `GenusCode` |
| `FamCode` | `FamId` — **join với Family để lấy Guid** |
| `Genus` | `GenusName` |

---

## Công cụ ETL — Python script

### Lý do chọn Python

- `polars` đọc parquet nhanh, zero-copy
- `psycopg2` / `sqlalchemy` upsert native vào PostgreSQL
- Dễ debug filter logic interactively
- Script chạy lại được (idempotent qua UPSERT)

### Cấu trúc thư mục

```
Pipeline/FishDexLocal/
├── parquetData/          ← đặt file .parquet ở đây
├── etl/
│   ├── config.py         ← connection string, paths, filter config
│   ├── filter.py         ← logic lọc spec_codes
│   ├── loaders/
│   │   ├── families.py
│   │   ├── species.py
│   │   ├── stocks.py
│   │   ├── ecology.py
│   │   ├── morph.py
│   │   ├── ecosystem.py
│   │   ├── occurrence.py
│   │   └── images.py
│   └── run.py            ← entry point, gọi theo thứ tự FK
├── requirements.txt      ← polars, psycopg2-binary, sqlalchemy
├── init.sql              ← pgvector extension
├── ETL_STRATEGY.md       ← file này
└── docker-compose.yml
```

### Nguyên tắc script

```python
# Mỗi loader dùng UPSERT để idempotent
INSERT INTO "Species" (...) VALUES (...)
ON CONFLICT ("SpecCode") DO UPDATE SET ...

# Wrap mỗi table trong transaction riêng
# → fail ở table nào thì rollback table đó, không cần rollback toàn bộ

# Log rõ ràng
print(f"[Species] Inserted: 2341, Skipped: 12, Errors: 0")
```

---

## Prod deployment

Sau khi verify data trên local:

```bash
# Dump data-only (không dump schema — PROD sẽ dùng EF migrations)
pg_dump \
  --host=localhost --port=5433 \
  --username=fishdex --dbname=fishdex \
  --data-only --no-owner --no-acl \
  --file=fishdex_seed_data.sql

# Restore lên PROD (sau khi chạy migrations trên PROD)
psql -h <prod-host> -U fishdex -d fishdex -f fishdex_seed_data.sql
```

**Hoặc** chạy ETL script thẳng lên PROD DB bằng cách đổi connection string trong `config.py`.

---

## Checklist trước khi viết ETL script

- [ ] Bổ sung file parquet vào `parquetData/`
- [ ] Chạy `species['Aquarium'].value_counts()` → xác nhận giá trị filter
- [ ] Confirm: lấy `'of minor importance'` không?
- [ ] Confirm: có lấy brackish water không (nếu cá phổ biến thủy sinh)?
- [ ] `docker compose up -d` và `dotnet ef database update` đã xong
