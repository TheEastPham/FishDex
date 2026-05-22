"""
Step 14: comnames.parquet → CommonNames.
PK: AutoCtr (từ source). UPSERT idempotent.
"""
from __future__ import annotations
import polars as pl
from ..config import PARQUET_DIR, PARQUET_FILES
from ..db import connect, to_str, to_int, to_bool, execute_upsert

SQL = """
    INSERT INTO "CommonNames" ("AutoCtr","SpecCode","StockCode","ComName",
        "Transliteration","CountryCode","Language","NameType","IsPreferred","Rank","Remarks")
    VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s)
    ON CONFLICT ("AutoCtr") DO UPDATE SET
        "ComName"         = EXCLUDED."ComName",
        "Transliteration" = EXCLUDED."Transliteration",
        "Language"        = EXCLUDED."Language",
        "IsPreferred"     = EXCLUDED."IsPreferred",
        "Rank"            = EXCLUDED."Rank"
"""


def load(spec_codes: set[int]):
    path = PARQUET_DIR / PARQUET_FILES["comnames"]
    if not path.exists():
        print(f"  [CommonNames] SKIP — {path} không tồn tại.")
        return

    df = pl.read_parquet(path)
    df = df.filter(pl.col("SpecCode").is_in(list(spec_codes)))

    rows = []
    for r in df.iter_rows(named=True):
        auto = to_int(r.get("autoctr") or r.get("AutoCtr"))
        if auto is None:
            continue
        rows.append((
            auto,
            to_int(r.get("SpecCode")),
            to_int(r.get("StockCode")),
            to_str(r.get("ComName")) or "",
            to_str(r.get("Transliteration")),
            to_str(r.get("C_Code") or r.get("CountryCode")),
            to_str(r.get("Language")),
            to_str(r.get("NameType")),
            to_bool(r.get("PreferredName")),
            to_int(r.get("Rank")) or 0,
            to_str(r.get("Remarks")),
        ))

    conn = connect()
    try:
        execute_upsert(conn, SQL, rows, "CommonNames")
    finally:
        conn.close()
