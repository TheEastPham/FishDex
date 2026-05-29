"""
Step 1: families.parquet → "Families" table.
PK: Id (Guid, generated here), FamCode unique index.
"""
from __future__ import annotations
import uuid
import polars as pl
from ..config import PARQUET_DIR, PARQUET_FILES
from ..db import connect, to_str, execute_upsert

SQL = """
    INSERT INTO "Families" ("Id", "FamCode", "Name", "CommonName", "BodyShapeI", "SwimMode", "ReproductiveGuild")
    VALUES (%s, %s, %s, %s, %s, %s, %s)
    ON CONFLICT ("FamCode") DO UPDATE SET
        "Name"             = EXCLUDED."Name",
        "CommonName"       = EXCLUDED."CommonName",
        "BodyShapeI"       = EXCLUDED."BodyShapeI",
        "SwimMode"         = EXCLUDED."SwimMode",
        "ReproductiveGuild"= EXCLUDED."ReproductiveGuild"
"""


def load():
    path = PARQUET_DIR / PARQUET_FILES["families"]
    if not path.exists():
        print(f"  [Families] SKIP — {path} không tồn tại.")
        return

    df = pl.read_parquet(path)
    rows = []
    for r in df.iter_rows(named=True):
        rows.append((
            str(uuid.uuid4()),
            r.get("FamCode"),
            to_str(r.get("Family")) or to_str(r.get("FamilyName")) or "",
            to_str(r.get("CommonName") or r.get("FBname")),
            to_str(r.get("BodyShapeI")),
            to_str(r.get("SwimMode")),
            to_str(r.get("ReprGuild") or r.get("ReproductiveGuild")),
        ))

    conn = connect()
    try:
        execute_upsert(conn, SQL, rows, "Families")
    finally:
        conn.close()
