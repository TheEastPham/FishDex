"""
Step 4-6: stocks.parquet → Stock + StockConservation + StockEnvironment
           + StockExternalRef + StockMetadata
Tất cả từ một parquet rộng (FishBase stocks table).
"""
from __future__ import annotations
import polars as pl
from ..config import PARQUET_DIR, PARQUET_FILES
from ..db import connect, to_str, to_float, to_int, to_bool, execute_upsert

# ── Stock ─────────────────────────────────────────────────────────────────────
SQL_STOCK = """
    INSERT INTO "Stocks" ("StockCode", "SpecCode", "SynOC", "StockDefs", "StockDefsGeneral", "Level", "LocalUnique")
    VALUES (%s,%s,%s,%s,%s,%s,%s)
    ON CONFLICT ("StockCode") DO UPDATE SET
        "SpecCode"         = EXCLUDED."SpecCode",
        "SynOC"            = EXCLUDED."SynOC",
        "StockDefs"        = EXCLUDED."StockDefs",
        "StockDefsGeneral" = EXCLUDED."StockDefsGeneral",
        "Level"            = EXCLUDED."Level",
        "LocalUnique"      = EXCLUDED."LocalUnique"
"""

# ── StockConservation ─────────────────────────────────────────────────────────
SQL_CONSERVATION = """
    INSERT INTO "StockConservations" ("StockCode","IUCN_Code","IUCN_Assessment","IUCN_DateAssessed",
        "IUCN_ID","IUCN_IDAssess","Protected","CITES_Code","CITES_Date","CITES_Ref","CITES_Remarks","CMS")
    VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s)
    ON CONFLICT ("StockCode") DO UPDATE SET
        "IUCN_Code"         = EXCLUDED."IUCN_Code",
        "IUCN_Assessment"   = EXCLUDED."IUCN_Assessment",
        "IUCN_DateAssessed" = EXCLUDED."IUCN_DateAssessed",
        "IUCN_ID"           = EXCLUDED."IUCN_ID",
        "IUCN_IDAssess"     = EXCLUDED."IUCN_IDAssess",
        "Protected"         = EXCLUDED."Protected",
        "CITES_Code"        = EXCLUDED."CITES_Code",
        "CITES_Date"        = EXCLUDED."CITES_Date",
        "CITES_Ref"         = EXCLUDED."CITES_Ref",
        "CITES_Remarks"     = EXCLUDED."CITES_Remarks",
        "CMS"               = EXCLUDED."CMS"
"""

# ── StockEnvironment ──────────────────────────────────────────────────────────
SQL_ENVIRONMENT = """
    INSERT INTO "StockEnvironments" ("StockCode",
        "Northernmost","NorthSouthN","Southermost","NorthSouthS",
        "Westernmost","WestEastW","Easternmost","WestEastE",
        "TempMin","TempMax","TempPreferred","PHMin","PHMax","DHMin","DHMax",
        "ResilienceRemark")
    VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s)
    ON CONFLICT ("StockCode") DO UPDATE SET
        "TempMin"         = EXCLUDED."TempMin",
        "TempMax"         = EXCLUDED."TempMax",
        "TempPreferred"   = EXCLUDED."TempPreferred",
        "PHMin"           = EXCLUDED."PHMin",
        "PHMax"           = EXCLUDED."PHMax",
        "DHMin"           = EXCLUDED."DHMin",
        "DHMax"           = EXCLUDED."DHMax",
        "ResilienceRemark"= EXCLUDED."ResilienceRemark"
"""

# ── StockExternalRef ──────────────────────────────────────────────────────────
SQL_EXTERNAL = """
    INSERT INTO "StockExternalRefs" ("StockCode",
        "GenBankID","RfeID","FIGIS_ID","EcotoxID","GMAD_ID","SAUP_ID","SAUP_Group","SAUP",
        "BOLD_ID","MitoRef","AusMuseum","FishTrace","IGFAName","EssayID","ICESStockID",
        "OsteoBaseID","DORIS_ID","FishipediaID","SocotraAtlasID","AFORO_ID","FishSounds_ID","StocksRefNo")
    VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s)
    ON CONFLICT ("StockCode") DO UPDATE SET
        "GenBankID"    = EXCLUDED."GenBankID",
        "BOLD_ID"      = EXCLUDED."BOLD_ID",
        "IGFAName"     = EXCLUDED."IGFAName",
        "StocksRefNo"  = EXCLUDED."StocksRefNo"
"""

# ── StockMetadata ─────────────────────────────────────────────────────────────
SQL_METADATA = """
    INSERT INTO "StockMetadatas" ("StockCode","Entered","DateEntered","Modified","DateModified","Expert","DateChecked")
    VALUES (%s,%s,%s,%s,%s,%s,%s)
    ON CONFLICT ("StockCode") DO UPDATE SET
        "Modified"    = EXCLUDED."Modified",
        "DateModified"= EXCLUDED."DateModified"
"""


def load(spec_codes: set[int]):
    path = PARQUET_DIR / PARQUET_FILES["stocks"]
    if not path.exists():
        print(f"  [Stocks] SKIP — {path} không tồn tại.")
        return

    df = pl.read_parquet(path)
    df = df.filter(pl.col("SpecCode").is_in(list(spec_codes)))

    stock_rows, cons_rows, env_rows, ext_rows, meta_rows = [], [], [], [], []

    for r in df.iter_rows(named=True):
        sc = r.get("StockCode")
        spec = r.get("SpecCode")

        stock_rows.append((
            sc, spec,
            to_str(r.get("SynOC")),
            to_str(r.get("StockDefs")),
            to_str(r.get("StockDefsGeneral")),
            to_str(r.get("Level")),
            to_bool(r.get("LocalUnique")),
        ))

        cons_rows.append((
            sc,
            to_str(r.get("IUCN_Code")),
            to_str(r.get("IUCN_Assessment")),
            r.get("IUCN_DateAssessed"),
            to_int(r.get("IUCN_ID")),
            to_int(r.get("IUCN_IDAssess")),
            to_bool(r.get("Protected")),
            to_str(r.get("CITES_Code")),
            r.get("CITES_Date"),
            to_str(r.get("CITES_Ref")),
            to_str(r.get("CITES_Remarks")),
            to_str(r.get("CMS")),
        ))

        env_rows.append((
            sc,
            to_float(r.get("Northernmost")),
            to_str(r.get("NorthSouthN")),
            to_float(r.get("Southermost")),
            to_str(r.get("NorthSouthS")),
            to_float(r.get("Westernmost")),
            to_str(r.get("WestEastW")),
            to_float(r.get("Easternmost")),
            to_str(r.get("WestEastE")),
            to_float(r.get("TempMin")),
            to_float(r.get("TempMax")),
            to_float(r.get("TempPreferred")),
            to_float(r.get("PHMin")),
            to_float(r.get("PHMax")),
            to_float(r.get("DHMin")),
            to_float(r.get("DHMax")),
            to_str(r.get("ResilienceRemark")),
        ))

        ext_rows.append((
            sc,
            to_str(r.get("GenBankID")), to_str(r.get("RfeID")),
            to_str(r.get("FIGIS_ID")), to_str(r.get("EcotoxID")),
            to_str(r.get("GMAD_ID")), to_str(r.get("SAUP_ID")),
            to_str(r.get("SAUP_Group")), to_str(r.get("SAUP")),
            to_str(r.get("BOLD_ID")), to_str(r.get("MitoRef")),
            to_str(r.get("AusMuseum")), to_str(r.get("FishTrace")),
            to_str(r.get("IGFAName")), to_str(r.get("EssayID")),
            to_str(r.get("ICESStockID")), to_str(r.get("OsteoBaseID")),
            to_str(r.get("DORIS_ID")), to_str(r.get("FishipediaID")),
            to_str(r.get("SocotraAtlasID")), to_str(r.get("AFORO_ID")),
            to_str(r.get("FishSounds_ID")), to_str(r.get("StocksRefNo")),
        ))

        meta_rows.append((
            sc,
            to_int(r.get("Entered")),
            r.get("DateEntered"),
            to_int(r.get("Modified")),
            r.get("DateModified"),
            to_int(r.get("Expert")),
            r.get("DateChecked"),
        ))

    conn = connect()
    try:
        execute_upsert(conn, SQL_STOCK, stock_rows, "Stocks")
        execute_upsert(conn, SQL_CONSERVATION, cons_rows, "StockConservations")
        execute_upsert(conn, SQL_ENVIRONMENT, env_rows, "StockEnvironments")
        execute_upsert(conn, SQL_EXTERNAL, ext_rows, "StockExternalRefs")
        execute_upsert(conn, SQL_METADATA, meta_rows, "StockMetadatas")
    finally:
        conn.close()
