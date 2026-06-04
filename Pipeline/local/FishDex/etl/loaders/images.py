"""
Step 15 (optional): pictures.parquet → SystemImages.
PK: Id (Guid, generated). Chiến lược: DELETE WHERE SpecCode IN spec_codes,
sau đó INSERT mới (idempotent).
"""
from __future__ import annotations
import uuid
import polars as pl
from ..config import PARQUET_DIR, PARQUET_FILES
from ..db import connect, to_str, to_float, to_int, to_bool, execute_upsert, delete_by_spec_codes

SQL = """
    INSERT INTO "SystemImages" ("Id","SpecCode","Name","PictureType",
        "LifeStage","Size","LengthType","BestPic","Score",
        "PicPreferred","PicPreferredMale","PicPreferredFem","PicPreferredJuv")
    VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s)
"""


def load(spec_codes: set[int]):
    path = PARQUET_DIR / PARQUET_FILES["images"]
    if not path.exists():
        print(f"  [SystemImages] SKIP — {path} không tồn tại.")
        return

    df = pl.read_parquet(path)
    df = df.filter(pl.col("SpecCode").is_in(list(spec_codes)))

    rows = []
    for r in df.iter_rows(named=True):
        rows.append((
            str(uuid.uuid4()),
            to_int(r.get("SpecCode")),
            to_str(r.get("PicName") or r.get("Name")) or "",
            to_str(r.get("PicType") or r.get("PictureType")) or "",
            to_str(r.get("LifeStage")),
            to_float(r.get("Size")),
            to_str(r.get("LengthType")),
            to_str(r.get("BestPic")),
            to_int(r.get("Score")),
            to_bool(r.get("PicPreferred")),
            to_bool(r.get("PicPreferredMale") or r.get("PicPrefMale")),
            to_bool(r.get("PicPreferredFem") or r.get("PicPrefFemale")),
            to_bool(r.get("PicPreferredJuv") or r.get("PicPrefJuv")),
        ))

    conn = connect()
    try:
        delete_by_spec_codes(conn, "SystemImages", "SpecCode", spec_codes)
        execute_upsert(conn, SQL, rows, "SystemImages")
    finally:
        conn.close()
