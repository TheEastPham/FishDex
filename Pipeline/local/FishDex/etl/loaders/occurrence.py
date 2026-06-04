"""
Step 13: occurrence.parquet → Occurrence.
PK Id (BIGSERIAL) auto-gen. Chiến lược: DELETE WHERE SpecCode IN spec_codes
trước rồi INSERT mới (idempotent).
"""
from __future__ import annotations
import polars as pl
from ..config import PARQUET_DIR, PARQUET_FILES
from ..db import connect, to_str, to_float, to_int, execute_upsert, delete_by_spec_codes

SQL = """
    INSERT INTO "Occurrences" ("SpecCode","CountryCode","Locality","Gazetteer",
        "LatitudeDec","LongitudeDec","Province")
    VALUES (%s,%s,%s,%s,%s,%s,%s)
"""


def load(spec_codes: set[int]):
    path = PARQUET_DIR / PARQUET_FILES["occurrence"]
    if not path.exists():
        print(f"  [Occurrences] SKIP — {path} không tồn tại.")
        return

    df = pl.read_parquet(path)
    df = df.filter(pl.col("SpecCode").is_in(list(spec_codes)))

    rows = []
    for r in df.iter_rows(named=True):
        lat = to_float(r.get("LatitudeDec"))
        lon = to_float(r.get("LongitudeDec"))
        if lat is None or lon is None:
            continue  # bỏ qua row thiếu toạ độ (entity yêu cầu double không null)
        rows.append((
            to_int(r.get("SpecCode")),
            to_str(r.get("C_Code") or r.get("CountryCode")),
            to_str(r.get("Locality")),
            to_str(r.get("Gazetteer")),
            lat, lon,
            to_str(r.get("Province")),
        ))

    conn = connect()
    try:
        delete_by_spec_codes(conn, "Occurrences", "SpecCode", spec_codes)
        execute_upsert(conn, SQL, rows, "Occurrences")
    finally:
        conn.close()
