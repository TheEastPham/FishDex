"""
Step 11-12: ecosystemref.parquet → EcosystemRef (load TOÀN BỘ, không filter)
            ecosystem.parquet    → Ecosystem (junction table, filter spec_codes)
"""
from __future__ import annotations
import polars as pl
from ..config import PARQUET_DIR, PARQUET_FILES
from ..db import connect, to_str, to_float, to_int, to_bool, execute_upsert

SQL_REF = """
    INSERT INTO "EcosystemRef" ("E_CODE","EcosystemName","EcosystemType","Location",
        "NorthernLat","SouthernLat","WesternLat","EasternLat",
        "Area","DrainageArea","RiverLength","Salinity","AverageDepth","MaxDepth",
        "TempSurface","TempDepth",
        "Polar","Boreal","Temperate","Subtropical","Tropical",
        "MEOW","LME","MPA","TotalCount","TotalFamCount",
        "Description","Comments","LastUpdate")
    VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s)
    ON CONFLICT ("E_CODE") DO UPDATE SET
        "EcosystemName" = EXCLUDED."EcosystemName",
        "EcosystemType" = EXCLUDED."EcosystemType",
        "Location"      = EXCLUDED."Location",
        "Description"   = EXCLUDED."Description"
"""

SQL_ECO = """
    INSERT INTO "Ecosystem" ("AutoCtr","E_CODE","SpecCode","StockCode",
        "Status","CurrentPresence","Abundance","LifeStage","Remarks",
        "EcosystemRefNo","Entered","Dateentered","Modified","Datemodified","TS")
    VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s)
    ON CONFLICT ("AutoCtr") DO UPDATE SET
        "Status"          = EXCLUDED."Status",
        "CurrentPresence" = EXCLUDED."CurrentPresence",
        "Abundance"       = EXCLUDED."Abundance"
"""


def load_refs():
    """Load tất cả EcosystemRef — không filter."""
    path = PARQUET_DIR / PARQUET_FILES["ecosystemref"]
    if not path.exists():
        print(f"  [EcosystemRefs] SKIP — {path} không tồn tại.")
        return

    df = pl.read_parquet(path)
    rows = []
    for r in df.iter_rows(named=True):
        rows.append((
            to_int(r.get("E_CODE")),
            to_str(r.get("EcosystemName")),
            to_str(r.get("EcosystemType")),
            to_str(r.get("Location")),
            to_float(r.get("NorthernLat")), to_float(r.get("SouthernLat")),
            to_float(r.get("WesternLat")), to_float(r.get("EasternLat")),
            to_float(r.get("Area")), to_float(r.get("DrainageArea")),
            to_float(r.get("RiverLength")), to_float(r.get("Salinity")),
            to_float(r.get("AverageDepth")), to_float(r.get("MaxDepth")),
            to_float(r.get("TempSurface")), to_float(r.get("TempDepth")),
            to_bool(r.get("Polar")), to_bool(r.get("Boreal")),
            to_bool(r.get("Temperate")), to_bool(r.get("Subtropical")),
            to_bool(r.get("Tropical")),
            to_str(r.get("MEOW")), to_str(r.get("LME")), to_str(r.get("MPA")),
            to_int(r.get("TotalCount")), to_int(r.get("TotalFamCount")),
            to_str(r.get("Description")), to_str(r.get("Comments")),
            r.get("LastUpdate"),
        ))

    conn = connect()
    try:
        execute_upsert(conn, SQL_REF, rows, "EcosystemRefs")
    finally:
        conn.close()


def load_junction(spec_codes: set[int]):
    """Load Ecosystem junction — filter theo SpecCode."""
    path = PARQUET_DIR / PARQUET_FILES["ecosystem"]
    if not path.exists():
        print(f"  [Ecosystems] SKIP — {path} không tồn tại.")
        return

    df = pl.read_parquet(path)
    df = df.filter(pl.col("Speccode").is_in(list(spec_codes)))

    rows = []
    for r in df.iter_rows(named=True):
        auto = to_int(r.get("autoctr") or r.get("AutoCtr"))
        if auto is None:
            continue
        rows.append((
            auto,
            to_int(r.get("E_CODE")),
            to_int(r.get("Speccode")),
            to_int(r.get("Stockcode")),
            to_str(r.get("Status")),
            to_str(r.get("CurrentPresence")),
            to_str(r.get("Abundance")),
            to_str(r.get("LifeStage")),
            to_str(r.get("Remarks")),
            to_int(r.get("EcosystemRefno")),
            to_str(r.get("Entered")),
            r.get("Dateentered") or r.get("DateEntered"),
            to_str(r.get("Modified")),
            r.get("Datemodified") or r.get("DateModified"),
            r.get("TS"),
        ))

    conn = connect()
    try:
        execute_upsert(conn, SQL_ECO, rows, "Ecosystems")
    finally:
        conn.close()
