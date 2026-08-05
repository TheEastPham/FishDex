"""
Step 12: species.parquet → "FishBaseSpeciesIndex".

Nạp TOÀN BỘ loài của FishBase (~35.700 dòng), KHÔNG lọc theo spec_codes — khác hẳn
loader species.py chỉ nạp 2.043 loài đã qua bộ lọc Aquarium.

Mục đích: khi người dùng tìm một loài không có trong danh sách market, hệ thống phải biết
loài đó có tồn tại trong FishBase hay không. Cột IsLoaded phân biệt:
    có + IsLoaded=true   → loài đã nạp, thêm thẳng vào danh sách quốc gia
    có + IsLoaded=false  → loài thật nhưng chưa nạp, cần chạy lại ETL cho SpecCode này
    không có             → loài lai, đi luồng community species

IsLoaded được tính lại mỗi lần chạy bằng cách đối chiếu với bảng "Species", không tin
giá trị cũ trong DB — vì bộ lọc Aquarium hoặc new_spec_codes.txt có thể đã thay đổi.
"""
from __future__ import annotations
import polars as pl
from ..config import PARQUET_DIR, PARQUET_FILES
from ..db import connect, to_str, to_int, to_bool, execute_upsert

SQL = """
    INSERT INTO "FishBaseSpeciesIndex"
        ("SpecCode","SpeciesName","Genus","FamCode","Fresh","Brack","Aquarium","IsLoaded")
    VALUES (%s,%s,%s,%s,%s,%s,%s,false)
    ON CONFLICT ("SpecCode") DO UPDATE SET
        "SpeciesName" = EXCLUDED."SpeciesName",
        "Genus"       = EXCLUDED."Genus",
        "FamCode"     = EXCLUDED."FamCode",
        "Fresh"       = EXCLUDED."Fresh",
        "Brack"       = EXCLUDED."Brack",
        "Aquarium"    = EXCLUDED."Aquarium"
"""

# Đối chiếu với bảng Species để biết loài nào đã thực sự nạp vào FishDex.
# Chạy sau upsert, một câu cho cả bảng — nhanh hơn set từng dòng.
SQL_SYNC_IS_LOADED = """
    UPDATE "FishBaseSpeciesIndex" i
    SET "IsLoaded" = EXISTS (
        SELECT 1 FROM "Species" s WHERE s."SpecCode" = i."SpecCode"
    )
"""


def load():
    # Dùng chung file với loader species — không cần thêm entry vào PARQUET_FILES.
    path = PARQUET_DIR / PARQUET_FILES["species"]
    if not path.exists():
        print(f"  [FishBaseSpeciesIndex] SKIP — {path} không tồn tại.")
        return

    df = pl.read_parquet(path)

    rows = []
    for r in df.iter_rows(named=True):
        spec_code = to_int(r.get("SpecCode"))
        if spec_code is None:
            continue

        genus = to_str(r.get("Genus")) or ""
        species = to_str(r.get("Species")) or ""
        species_name = f"{genus} {species}".strip()
        if not species_name:
            continue

        rows.append((
            spec_code,
            species_name,
            to_str(r.get("Genus")),
            to_int(r.get("FamCode")),
            to_bool(r.get("Fresh")),
            to_bool(r.get("Brack")),
            to_str(r.get("Aquarium")),
        ))

    conn = connect()
    try:
        execute_upsert(conn, SQL, rows, "FishBaseSpeciesIndex")

        with conn.cursor() as cur:
            cur.execute(SQL_SYNC_IS_LOADED)
        conn.commit()

        with conn.cursor() as cur:
            cur.execute('SELECT count(*) FROM "FishBaseSpeciesIndex" WHERE "IsLoaded"')
            loaded = cur.fetchone()[0]
        print(f"  [FishBaseSpeciesIndex] IsLoaded=true: {loaded:,} / {len(rows):,}")
    finally:
        conn.close()
