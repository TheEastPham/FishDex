"""
Step 2: genera.parquet → "Genuses" table.
PK: GenusCode (int). FK: FamId (Guid) → phải lookup từ Families sau step 1.
"""
from __future__ import annotations
import polars as pl
from ..config import PARQUET_DIR, PARQUET_FILES
from ..db import connect, to_str, execute_upsert

SQL = """
    INSERT INTO "Genuses" ("GenusCode", "FamId", "GenusName")
    VALUES (%s, %s, %s)
    ON CONFLICT ("GenusCode") DO UPDATE SET
        "FamId"    = EXCLUDED."FamId",
        "GenusName"= EXCLUDED."GenusName"
"""


def load():
    path = PARQUET_DIR / PARQUET_FILES["genera"]
    if not path.exists():
        print(f"  [Genuses] SKIP — {path} không tồn tại.")
        return

    conn = connect()
    try:
        # Build FamCode → FamId (Guid) lookup từ DB (đã load step 1)
        with conn.cursor() as cur:
            cur.execute('SELECT "FamCode", "Id" FROM "Families"')
            fam_lookup: dict[int, str] = {row[0]: row[1] for row in cur.fetchall()}

        df = pl.read_parquet(path)
        rows = []
        skipped_fam = 0
        for r in df.iter_rows(named=True):
            fam_id = fam_lookup.get(r.get("FamCode"))
            if fam_id is None:
                skipped_fam += 1
                continue
            rows.append((
                r.get("GenCode"),
                fam_id,
                to_str(r.get("Genus") or r.get("GenusName")) or "",
            ))

        if skipped_fam:
            print(f"  [Genuses] Bỏ qua {skipped_fam:,} genera không tìm thấy FamCode.")
        execute_upsert(conn, SQL, rows, "Genuses")
    finally:
        conn.close()
