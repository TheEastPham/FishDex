"""
Step 3: species.parquet → "Species" table.
Lọc bằng spec_codes (Fresh=1 + Aquarium filter).
PK: Id (Guid, generated here). SpecCode: unique index, dùng làm FK target.
"""
from __future__ import annotations
import uuid
import polars as pl
from ..config import PARQUET_DIR, PARQUET_FILES
from ..db import connect, to_str, to_float, to_int, execute_upsert

# TODO(Story 1.9a): Thêm "LongevityCaptive" vào SQL + rows khi re-run ETL
#   SQL: thêm "LongevityCaptive" vào INSERT list (sau "LongevityWild") và ON CONFLICT DO UPDATE
#   rows: thêm to_float(r.get("LongevityCapt") or r.get("LongevityCaptive"))
#   Parquet cols: species.parquet — kiểm tra tên cột bằng: pl.read_parquet(path).columns
SQL = """
    INSERT INTO "Species" (
        "Id", "SpecCode", "GenusCode", "FamCode", "FamId",
        "WaterType", "SpeciesName", "SpeciesRefNo", "Author", "BodyShapeI",
        "Source", "AuthorRef", "Remark", "TaxIssue",
        "Length", "Weight", "Comments", "Dangerous",
        "Vulnerability", "VulnerabilityClimate", "AirBreathing", "LifeCycle",
        "DemersPelag", "MaxLengthRef", "LengthFemale", "LongevityWild",
        "PicPreferredNameM", "PicPreferredNameF"
    ) VALUES (
        %s,%s,%s,%s,%s,
        %s,%s,%s,%s,%s,
        %s,%s,%s,%s,
        %s,%s,%s,%s,
        %s,%s,%s,%s,
        %s,%s,%s,%s,
        %s,%s
    )
    ON CONFLICT ("SpecCode") DO UPDATE SET
        "GenusCode"          = EXCLUDED."GenusCode",
        "FamCode"            = EXCLUDED."FamCode",
        "FamId"              = EXCLUDED."FamId",
        "WaterType"          = EXCLUDED."WaterType",
        "SpeciesName"        = EXCLUDED."SpeciesName",
        "SpeciesRefNo"       = EXCLUDED."SpeciesRefNo",
        "Author"             = EXCLUDED."Author",
        "BodyShapeI"         = EXCLUDED."BodyShapeI",
        "Source"             = EXCLUDED."Source",
        "AuthorRef"          = EXCLUDED."AuthorRef",
        "Remark"             = EXCLUDED."Remark",
        "TaxIssue"           = EXCLUDED."TaxIssue",
        "Length"             = EXCLUDED."Length",
        "Weight"             = EXCLUDED."Weight",
        "Comments"           = EXCLUDED."Comments",
        "Dangerous"          = EXCLUDED."Dangerous",
        "Vulnerability"      = EXCLUDED."Vulnerability",
        "VulnerabilityClimate"= EXCLUDED."VulnerabilityClimate",
        "AirBreathing"       = EXCLUDED."AirBreathing",
        "LifeCycle"          = EXCLUDED."LifeCycle",
        "DemersPelag"        = EXCLUDED."DemersPelag",
        "MaxLengthRef"       = EXCLUDED."MaxLengthRef",
        "LengthFemale"       = EXCLUDED."LengthFemale",
        "LongevityWild"      = EXCLUDED."LongevityWild",
        "PicPreferredNameM"  = EXCLUDED."PicPreferredNameM",
        "PicPreferredNameF"  = EXCLUDED."PicPreferredNameF"
"""

# WaterType enum: Unknown=0, Freshwater=1, Saltwater=2, Brackish=3
def _water_type(fresh, brack, salt) -> int:
    if int(fresh or 0):
        return 1
    if int(brack or 0):
        return 3
    if int(salt or 0):
        return 2
    return 0


def load(spec_codes: set[int]):
    path = PARQUET_DIR / PARQUET_FILES["species"]
    if not path.exists():
        print(f"  [Species] SKIP — {path} không tồn tại.")
        return

    conn = connect()
    try:
        with conn.cursor() as cur:
            cur.execute('SELECT "FamCode", "Id" FROM "Families"')
            fam_lookup: dict[int, str] = {row[0]: row[1] for row in cur.fetchall()}

        df = pl.read_parquet(path)
        df = df.filter(pl.col("SpecCode").is_in(list(spec_codes)))

        rows = []
        for r in df.iter_rows(named=True):
            fam_code = to_int(r.get("FamCode")) or 0
            fam_id = fam_lookup.get(fam_code, str(uuid.uuid4()))  # fallback nếu chưa có
            genus = to_str(r.get("Genus")) or ""
            species = to_str(r.get("Species")) or ""
            species_name = f"{genus} {species}".strip()

            rows.append((
                str(uuid.uuid4()),
                r.get("SpecCode"),
                to_int(r.get("GenCode")),
                fam_code,
                fam_id,
                _water_type(r.get("Fresh"), r.get("Brack"), r.get("Saltwater")),
                species_name,
                to_str(r.get("SpeciesRefNo")),
                to_str(r.get("Author")),
                to_str(r.get("BodyShapeI")),
                to_str(r.get("Source")),
                to_str(r.get("AuthorRef")),
                to_str(r.get("Remark")),
                to_str(r.get("TaxIssue")),
                to_float(r.get("Length")),
                to_float(r.get("Weight")),
                to_str(r.get("Comments")),
                to_str(r.get("Dangerous")),
                to_int(r.get("Vulnerability")),
                to_int(r.get("VulnerabilityClimate")),
                to_str(r.get("AirBreathing")),
                to_str(r.get("LifeCycle")),
                to_str(r.get("DemersPelag")),
                to_str(r.get("MaxLengthRef")),
                to_float(r.get("LengthFemale")),
                to_float(r.get("LongevityWild")),
                to_str(r.get("PicPreferredNameM")),
                to_str(r.get("PicPreferredNameF")),
            ))

        execute_upsert(conn, SQL, rows, "Species")
    finally:
        conn.close()
