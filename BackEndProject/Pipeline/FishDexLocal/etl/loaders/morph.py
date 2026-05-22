"""
Step 10: morphdat.parquet → MorphData + MorphTeeth + MorphPigmentation
         + MorphFins + MorphMeristics + MorphMetrics
PK của tất cả sub-table là StockCode (1-to-1 với MorphData).
"""
from __future__ import annotations
import polars as pl
from ..config import PARQUET_DIR, PARQUET_FILES
from ..db import connect, to_str, to_float, to_int, execute_upsert

SQL_MORPH = """
    INSERT INTO "MorphData" ("StockCode","Speccode","MorphDatRefNo","AppearancePic","EaseofID",
        "BodyShapeI","BodyShapeII","Forehead","OperculumPresent","TypeofEyes","TypeofMouth","PosofMouth",
        "GasBladder","SexualAttributes","SexMorphology","RemarkSex","Females","Males",
        "Entered","DateEntered","Modified","DateModified","Expert","DateChecked","TS")
    VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s)
    ON CONFLICT ("StockCode") DO UPDATE SET
        "BodyShapeI"  = EXCLUDED."BodyShapeI",
        "BodyShapeII" = EXCLUDED."BodyShapeII",
        "TypeofMouth" = EXCLUDED."TypeofMouth"
"""

SQL_TEETH = """
    INSERT INTO "MorphTeeth" ("StockCode",
        "MandibleTeeth","MandibleTeethT1","MandibleTeethT2","MandibleRowsMin","MandibleRowsMax",
        "MaxillaTeeth","MaxillaTeethT1","MaxillaTeethT2","MaxillaRowsMin","MaxillaRowsMax",
        "VomerineTeeth","VomerineTeethT1","VomerineTeethT2","VomerineRowsMin","VomerineRowsMax",
        "Palatine","PalatineTeethT1","PalatineTeethT2","PalatineRowsMin","PalatineRowsMax",
        "PharyngealTeeth","PharyngealTeethT1","PharyngealTeethT2","PharyngealRowsMin","PharyngealRowsMax",
        "TeethonTongue","Lipsteeth","DermalTeeth","CommentonTeeth")
    VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s)
    ON CONFLICT ("StockCode") DO UPDATE SET
        "MandibleTeeth" = EXCLUDED."MandibleTeeth"
"""

SQL_PIG = """
    INSERT INTO "MorphPigmentations" ("StockCode","SexColors","StrikingFeatures",
        "HorStripesTTI","HorStripesTTII","VerStripesTTI","VerStripesTTII","VerStripesTTIII",
        "DiaStripesTTI","DiaStripesTTII","DiaStripesTTIII",
        "CurStripesTTI","CurStripesTTII","CurStripesTTIII",
        "SpotsTTI","SpotsTTII","SpotsTTIII")
    VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s)
    ON CONFLICT ("StockCode") DO UPDATE SET
        "StrikingFeatures" = EXCLUDED."StrikingFeatures"
"""

SQL_FINS = """
    INSERT INTO "MorphFins" ("StockCode",
        "Dfinno","DorsalFinI","DorsalFinII","DorsalAttributes",
        "DorsalSpinesMin","DorsalSpinesMax","DorsalSoftRaysMin","DorsalSoftRaysMax",
        "Notched","DFinletsmin","DFinletsmax","Adifin",
        "Afinno","AnalFinI","AnalFinII","AnalFinSpinesMin","AnalFinSpinesMax","Araymin","Araymax",
        "PectoralAttributes","Pspines2","Praymin","Praymax",
        "PelvicsAttributes","VPosition","VPosition2","Vspines","Vraymin","Vraymax",
        "CaudalFinI","CaudalFinII","CShape","Attributes")
    VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s)
    ON CONFLICT ("StockCode") DO UPDATE SET
        "DorsalFinI" = EXCLUDED."DorsalFinI",
        "CaudalFinI" = EXCLUDED."CaudalFinI"
"""

SQL_MERISTICS = """
    INSERT INTO "MorphMeristics" ("StockCode",
        "TypeofScales","Scutes","Keels","LateralLinesNo","LLinterrupted",
        "ScalesLateralmin","ScalesLateralmax","PoredScalesMin","PoredScalesMax",
        "LatSeriesMin","LatSeriesMax","ScaleRowsAboveMin","ScaleRowsAboveMax",
        "ScaleRowsBelowMin","ScaleRowsBelowMax","ScalesPeduncMin","ScalesPeduncMax",
        "BarbelsNo","BarbelsType","GillCleftsNo","Spiracle",
        "GillRakersLowMin","GillRakersLowMax","GillRakersUpMin","GillRakersUpMax",
        "GillRakersTotalMin","GillRakersTotalMax",
        "Vertebrae","VertebraePreanMin","VertebraePreanMax","VertebraeTotalMin","VertebraeTotalMax")
    VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s)
    ON CONFLICT ("StockCode") DO UPDATE SET
        "TypeofScales" = EXCLUDED."TypeofScales"
"""

SQL_METRICS = """
    INSERT INTO "MorphMetrics" ("StockCode",
        "StandardLengthCm","Forklength","Totallength","HeadLength",
        "PreDorsalLength","PrePelvicsLength","PreAnalLength","PreorbitalLength","EyeLength",
        "PeduncleLength","PostHeadDepth","PostTrunkDepth","MaximumDepth","PeduncleDepth","CaudalHeight",
        "SimilarSpecies1","SimilarSpec1Remarks","SimilarSpecies2","SimilarSpec2Remarks",
        "OtherRef1","OtherRef2","AddChars")
    VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s)
    ON CONFLICT ("StockCode") DO UPDATE SET
        "StandardLengthCm" = EXCLUDED."StandardLengthCm",
        "Totallength"      = EXCLUDED."Totallength"
"""


def load(spec_codes: set[int]):
    path = PARQUET_DIR / PARQUET_FILES["morphdat"]
    if not path.exists():
        print(f"  [MorphData] SKIP — {path} không tồn tại.")
        return

    df = pl.read_parquet(path)
    # MorphData lọc theo Speccode (lowercase trong entity) hoặc SpecCode
    col = "SpecCode" if "SpecCode" in df.columns else "Speccode"
    df = df.filter(pl.col(col).is_in(list(spec_codes)))

    morph_rows, teeth_rows, pig_rows = [], [], []
    fins_rows, meri_rows, metrics_rows = [], [], []

    for r in df.iter_rows(named=True):
        sc = r.get("StockCode")
        speccode = r.get("SpecCode") or r.get("Speccode")

        morph_rows.append((
            sc, speccode,
            to_str(r.get("MorphDatRefNo")),
            to_str(r.get("AppearancePic")),
            to_str(r.get("EaseofID")),
            to_str(r.get("BodyShapeI")), to_str(r.get("BodyShapeII")),
            to_str(r.get("Forehead")), to_str(r.get("OperculumPresent")),
            to_str(r.get("TypeofEyes")), to_str(r.get("TypeofMouth")), to_str(r.get("PosofMouth")),
            to_str(r.get("GasBladder")), to_str(r.get("SexualAttributes")),
            to_str(r.get("SexMorphology")), to_str(r.get("RemarkSex")),
            to_int(r.get("Females")), to_int(r.get("Males")),
            to_int(r.get("Entered")), r.get("DateEntered"),
            to_int(r.get("Modified")), r.get("DateModified"),
            to_int(r.get("Expert")), r.get("DateChecked"), r.get("TS"),
        ))

        teeth_rows.append((
            sc,
            to_str(r.get("MandibleTeeth")), to_str(r.get("MandibleTeethT1")), to_str(r.get("MandibleTeethT2")),
            to_str(r.get("MandibleRowsMin")), to_str(r.get("MandibleRowsMax")),
            to_str(r.get("MaxillaTeeth")), to_str(r.get("MaxillaTeethT1")), to_str(r.get("MaxillaTeethT2")),
            to_str(r.get("MaxillaRowsMin")), to_str(r.get("MaxillaRowsMax")),
            to_str(r.get("VomerineTeeth")), to_str(r.get("VomerineTeethT1")), to_str(r.get("VomerineTeethT2")),
            to_str(r.get("VomerineRowsMin")), to_str(r.get("VomerineRowsMax")),
            to_str(r.get("Palatine")), to_str(r.get("PalatineTeethT1")), to_str(r.get("PalatineTeethT2")),
            to_str(r.get("PalatineRowsMin")), to_str(r.get("PalatineRowsMax")),
            to_str(r.get("PharyngealTeeth")), to_str(r.get("PharyngealTeethT1")), to_str(r.get("PharyngealTeethT2")),
            to_str(r.get("PharyngealRowsMin")), to_str(r.get("PharyngealRowsMax")),
            to_str(r.get("TeethonTongue")), to_str(r.get("Lipsteeth")),
            to_str(r.get("DermalTeeth")), to_str(r.get("CommentonTeeth")),
        ))

        pig_rows.append((
            sc,
            to_str(r.get("SexColors")), to_str(r.get("StrikingFeatures")),
            to_str(r.get("HorStripesTTI")), to_str(r.get("HorStripesTTII")),
            to_str(r.get("VerStripesTTI")), to_str(r.get("VerStripesTTII")), to_str(r.get("VerStripesTTIII")),
            to_str(r.get("DiaStripesTTI")), to_str(r.get("DiaStripesTTII")), to_str(r.get("DiaStripesTTIII")),
            to_str(r.get("CurStripesTTI")), to_str(r.get("CurStripesTTII")), to_str(r.get("CurStripesTTIII")),
            to_str(r.get("SpotsTTI")), to_str(r.get("SpotsTTII")), to_str(r.get("SpotsTTIII")),
        ))

        fins_rows.append((
            sc,
            to_int(r.get("Dfinno")),
            to_str(r.get("DorsalFinI")), to_str(r.get("DorsalFinII")),
            to_str(r.get("DorsalAttributes")),
            to_int(r.get("DorsalSpinesMin")), to_int(r.get("DorsalSpinesMax")),
            to_int(r.get("DorsalSoftRaysMin")), to_int(r.get("DorsalSoftRaysMax")),
            to_str(r.get("Notched")),
            to_int(r.get("DFinletsmin")), to_int(r.get("DFinletsmax")),
            to_str(r.get("Adifin")),
            to_int(r.get("Afinno")),
            to_str(r.get("AnalFinI")), to_str(r.get("AnalFinII")),
            to_int(r.get("AnalFinSpinesMin")), to_int(r.get("AnalFinSpinesMax")),
            to_int(r.get("Araymin")), to_int(r.get("Araymax")),
            to_str(r.get("PectoralAttributes")), to_str(r.get("Pspines2")),
            to_int(r.get("Praymin")), to_int(r.get("Praymax")),
            to_str(r.get("PelvicsAttributes")),
            to_str(r.get("VPosition")), to_str(r.get("VPosition2")),
            to_str(r.get("Vspines")),
            to_int(r.get("Vraymin")), to_int(r.get("Vraymax")),
            to_str(r.get("CaudalFinI")), to_str(r.get("CaudalFinII")),
            to_str(r.get("CShape")), to_str(r.get("Attributes")),
        ))

        meri_rows.append((
            sc,
            to_str(r.get("TypeofScales")), to_str(r.get("Scutes")), to_str(r.get("Keels")),
            to_int(r.get("LateralLinesNo")), to_str(r.get("LLinterrupted")),
            to_int(r.get("ScalesLateralmin")), to_int(r.get("ScalesLateralmax")),
            to_int(r.get("PoredScalesMin")), to_int(r.get("PoredScalesMax")),
            to_int(r.get("LatSeriesMin")), to_int(r.get("LatSeriesMax")),
            to_int(r.get("ScaleRowsAboveMin")), to_int(r.get("ScaleRowsAboveMax")),
            to_int(r.get("ScaleRowsBelowMin")), to_int(r.get("ScaleRowsBelowMax")),
            to_int(r.get("ScalesPeduncMin")), to_int(r.get("ScalesPeduncMax")),
            to_int(r.get("BarbelsNo")), to_str(r.get("BarbelsType")),
            to_int(r.get("GillCleftsNo")), to_str(r.get("Spiracle")),
            to_int(r.get("GillRakersLowMin")), to_int(r.get("GillRakersLowMax")),
            to_int(r.get("GillRakersUpMin")), to_int(r.get("GillRakersUpMax")),
            to_int(r.get("GillRakersTotalMin")), to_int(r.get("GillRakersTotalMax")),
            to_str(r.get("Vertebrae")),
            to_int(r.get("VertebraePreanMin")), to_int(r.get("VertebraePreanMax")),
            to_int(r.get("VertebraeTotalMin")), to_int(r.get("VertebraeTotalMax")),
        ))

        metrics_rows.append((
            sc,
            to_float(r.get("StandardLengthCm")), to_float(r.get("Forklength")),
            to_float(r.get("Totallength")), to_float(r.get("HeadLength")),
            to_float(r.get("PreDorsalLength")), to_float(r.get("PrePelvicsLength")),
            to_float(r.get("PreAnalLength")), to_float(r.get("PreorbitalLength")),
            to_float(r.get("EyeLength")), to_float(r.get("PeduncleLength")),
            to_float(r.get("PostHeadDepth")), to_float(r.get("PostTrunkDepth")),
            to_float(r.get("MaximumDepth")), to_float(r.get("PeduncleDepth")),
            to_float(r.get("CaudalHeight")),
            to_str(r.get("SimilarSpecies1")), to_str(r.get("SimilarSpec1Remarks")),
            to_str(r.get("SimilarSpecies2")), to_str(r.get("SimilarSpec2Remarks")),
            to_str(r.get("OtherRef1")), to_str(r.get("OtherRef2")),
            to_str(r.get("AddChars")),
        ))

    conn = connect()
    try:
        execute_upsert(conn, SQL_MORPH, morph_rows, "MorphData")
        execute_upsert(conn, SQL_TEETH, teeth_rows, "MorphTeeth")
        execute_upsert(conn, SQL_PIG, pig_rows, "MorphPigmentations")
        execute_upsert(conn, SQL_FINS, fins_rows, "MorphFins")
        execute_upsert(conn, SQL_MERISTICS, meri_rows, "MorphMeristics")
        execute_upsert(conn, SQL_METRICS, metrics_rows, "MorphMetrics")
    finally:
        conn.close()
