"""
Step 7-9: ecology.parquet → Ecology + HabitatZone + FeedingAndDiet
          + Associations + Substrate + SpecialHabitat + CircadianBehavior

Tất cả từ một parquet rộng. Dùng autoctr (từ parquet) làm shared key cho mọi
sub-table (EcologyId = autoctr, HabitatZoneId = autoctr, ...).
"""
from __future__ import annotations
import polars as pl
from ..config import PARQUET_DIR, PARQUET_FILES
from ..db import connect, to_str, to_float, to_int, to_bool, execute_upsert

# ── Ecology ───────────────────────────────────────────────────────────────────
SQL_ECOLOGY = """
    INSERT INTO "Ecologies" ("EcologyId","SpecCode","StockCode","EcologyRefNo","autoctr",
        "Entered","Dateentered","Modified","Datemodified","Expert","Datechecked","TS")
    VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s)
    ON CONFLICT ("EcologyId") DO UPDATE SET
        "SpecCode"     = EXCLUDED."SpecCode",
        "StockCode"    = EXCLUDED."StockCode",
        "EcologyRefNo" = EXCLUDED."EcologyRefNo",
        "autoctr"      = EXCLUDED."autoctr"
"""

# ── HabitatZone ───────────────────────────────────────────────────────────────
SQL_HABITAT = """
    INSERT INTO "HabitatZones" ("HabitatZoneId","EcologyId","HabitatsRef",
        "Neritic","SupraLittoralZone","Saltmarshes","LittoralZone","TidePools","Intertidal","SubLittoral",
        "Caves","Oceanic","Epipelagic","Mesopelagic","Bathypelagic","Abyssopelagic","Hadopelagic",
        "Estuaries","Mangroves","MarshesSwamps","CaveAnchialine","Stream","Lakes","Cave","Cave2")
    VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s)
    ON CONFLICT ("HabitatZoneId") DO UPDATE SET
        "Stream"     = EXCLUDED."Stream",
        "Lakes"      = EXCLUDED."Lakes",
        "Estuaries"  = EXCLUDED."Estuaries"
"""

# ── FeedingAndDiet ────────────────────────────────────────────────────────────
SQL_FEEDING = """
    INSERT INTO "FeedingAndDiets" ("FeedingId","EcologyId",
        "Herbivory2","HerbivoryRef","FeedingType","FeedingTypeRef",
        "DietTroph","DietSeTroph","DietTLu","DietseTLu","DietRemark","DietRef",
        "FoodTroph","FoodSeTroph","FoodRemark","FoodRef","AddRems")
    VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s)
    ON CONFLICT ("FeedingId") DO UPDATE SET
        "FeedingType" = EXCLUDED."FeedingType",
        "DietTroph"   = EXCLUDED."DietTroph",
        "FoodTroph"   = EXCLUDED."FoodTroph"
"""

# ── Associations ──────────────────────────────────────────────────────────────
SQL_ASSOC = """
    INSERT INTO "Associations" ("AssociationId","EcologyId","AssociationRef",
        "Parasitism","Solitary","Symbiosis","Symphorism","Commensalism","Mutualism","Epiphytic",
        "Schooling","SchoolingFrequency","SchoolingLifestage",
        "Shoaling","ShoalingFrequency","ShoalingLifestage","SchoolShoalRef",
        "AssociationsWith","AssociationsRemarks",
        "OutsideHost","OHRemarks","InsideHost","IHRemarks")
    VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s)
    ON CONFLICT ("AssociationId") DO UPDATE SET
        "Schooling"  = EXCLUDED."Schooling",
        "Shoaling"   = EXCLUDED."Shoaling"
"""

# ── Substrate ─────────────────────────────────────────────────────────────────
SQL_SUBSTRATE = """
    INSERT INTO "Substrates" ("SubstrateId","EcologyId","SubstrateRef",
        "Benthic","Sessile","Mobile","Demersal","Endofauna","Pelagic",
        "Megabenthos","Macrobenthos","Meiobenthos",
        "SoftBottom","Sand","Coarse","Fine","Level","Sloping",
        "Silt","Mud","Ooze","Detritus","Organic",
        "HardBottom","Rocky","Rubble","Gravel")
    VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s)
    ON CONFLICT ("SubstrateId") DO UPDATE SET
        "Benthic" = EXCLUDED."Benthic",
        "Pelagic" = EXCLUDED."Pelagic"
"""

# ── SpecialHabitat ────────────────────────────────────────────────────────────
SQL_SPECIAL = """
    INSERT INTO "SpecialHabitats" ("SpecialHabitatId","EcologyId","SpecialHabitatRef",
        "Macrophyte","BedsBivalve","BedsRock","SeaGrassBeds","BedsOthers",
        "CoralReefs","ReefExclusive","DropOffs","ReefFlats","Lagoons",
        "Burrows","Tunnels","Guyots","Crevices","Seamounts",
        "ColdSeeps","HydrothermalVents","DeepWaterCorals",
        "Vegetation","Leaves","Stems","Roots","Driftwood",
        "OInverterbrates","OIRemarks","Verterbrates","VRemarks",
        "Pilings","RicePaddies","BoatHulls",
        "Corals","SoftCorals","OnPolyp","BetweenPolyps","HardCorals","OnExoskeleton","InterstitialSpaces")
    VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s)
    ON CONFLICT ("SpecialHabitatId") DO UPDATE SET
        "CoralReefs" = EXCLUDED."CoralReefs",
        "Lagoons"    = EXCLUDED."Lagoons"
"""

# ── CircadianBehavior ─────────────────────────────────────────────────────────
SQL_CIRCADIAN = """
    INSERT INTO "CircadianBehaviors" ("CircadianId","EcologyId",
        "Circadian1","Circadian2","Circadian3",
        "BioAspect1","BioAspect2","BioAspect3",
        "RemarksCircadian","CircadianRef","CircadianAlsoRef")
    VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s)
    ON CONFLICT ("CircadianId") DO UPDATE SET
        "Circadian1" = EXCLUDED."Circadian1",
        "Circadian2" = EXCLUDED."Circadian2"
"""


def load(spec_codes: set[int]):
    path = PARQUET_DIR / PARQUET_FILES["ecology"]
    if not path.exists():
        print(f"  [Ecology] SKIP — {path} không tồn tại.")
        return

    df = pl.read_parquet(path)
    df = df.filter(pl.col("SpecCode").is_in(list(spec_codes)))

    ecology_rows, habitat_rows, feeding_rows = [], [], []
    assoc_rows, substrate_rows, special_rows, circadian_rows = [], [], [], []

    for r in df.iter_rows(named=True):
        auto = to_int(r.get("autoctr")) or to_int(r.get("AutoCtr"))
        if auto is None:
            continue
        spec = r.get("SpecCode")
        stock = to_str(r.get("StockCode"))

        # Ecology core
        ecology_rows.append((
            auto, spec, stock,
            to_str(r.get("EcologyRefNo")),
            auto,
            to_str(r.get("Entered")),
            r.get("DateEntered") or r.get("Dateentered"),
            to_str(r.get("Modified")),
            r.get("DateModified") or r.get("Datemodified"),
            to_str(r.get("Expert")),
            r.get("DateChecked") or r.get("Datechecked"),
            r.get("TS"),
        ))

        # HabitatZone
        habitat_rows.append((
            auto, auto,
            to_str(r.get("HabitatsRef")),
            to_bool(r.get("Neritic")), to_bool(r.get("SupraLittoralZone")), to_bool(r.get("Saltmarshes")),
            to_bool(r.get("LittoralZone")), to_bool(r.get("TidePools")), to_bool(r.get("Intertidal")),
            to_bool(r.get("SubLittoral")), to_bool(r.get("Caves")), to_bool(r.get("Oceanic")),
            to_bool(r.get("Epipelagic")), to_bool(r.get("Mesopelagic")), to_bool(r.get("Bathypelagic")),
            to_bool(r.get("Abyssopelagic")), to_bool(r.get("Hadopelagic")),
            to_bool(r.get("Estuaries")), to_bool(r.get("Mangroves")), to_bool(r.get("MarshesSwamps")),
            to_bool(r.get("CaveAnchialine")), to_bool(r.get("Stream")), to_bool(r.get("Lakes")),
            to_bool(r.get("Cave")), to_bool(r.get("Cave2")),
        ))

        # FeedingAndDiet
        feeding_rows.append((
            auto, auto,
            to_bool(r.get("Herbivory2")), to_str(r.get("HerbivoryRef")),
            to_str(r.get("FeedingType")), to_str(r.get("FeedingTypeRef")),
            to_float(r.get("DietTroph")) or 0, to_float(r.get("DietSeTroph")) or 0,
            to_float(r.get("DietTLu")) or 0, to_float(r.get("DietseTLu")) or 0,
            to_str(r.get("DietRemark")), to_str(r.get("DietRef")),
            to_float(r.get("FoodTroph")) or 0, to_float(r.get("FoodSeTroph")) or 0,
            to_str(r.get("FoodRemark")), to_str(r.get("FoodRef")),
            to_str(r.get("AddRems")),
        ))

        # Associations
        assoc_rows.append((
            auto, auto, to_str(r.get("AssociationRef")),
            to_bool(r.get("Parasitism")), to_bool(r.get("Solitary")),
            to_bool(r.get("Symbiosis")), to_bool(r.get("Symphorism")),
            to_bool(r.get("Commensalism")), to_bool(r.get("Mutualism")),
            to_bool(r.get("Epiphytic")),
            to_bool(r.get("Schooling")),
            to_str(r.get("SchoolingFrequency")), to_str(r.get("SchoolingLifestage")),
            to_bool(r.get("Shoaling")),
            to_str(r.get("ShoalingFrequency")), to_str(r.get("ShoalingLifestage")),
            to_str(r.get("SchoolShoalRef")),
            to_str(r.get("AssociationsWith")), to_str(r.get("AssociationsRemarks")),
            to_bool(r.get("OutsideHost")), to_str(r.get("OHRemarks")),
            to_bool(r.get("InsideHost")), to_str(r.get("IHRemarks")),
        ))

        # Substrate
        substrate_rows.append((
            auto, auto, to_str(r.get("SubstrateRef")),
            to_bool(r.get("Benthic")), to_bool(r.get("Sessile")), to_bool(r.get("Mobile")),
            to_bool(r.get("Demersal")), to_bool(r.get("Endofauna")), to_bool(r.get("Pelagic")),
            to_bool(r.get("Megabenthos")), to_bool(r.get("Macrobenthos")), to_bool(r.get("Meiobenthos")),
            to_bool(r.get("SoftBottom")), to_bool(r.get("Sand")), to_bool(r.get("Coarse")),
            to_bool(r.get("Fine")), to_bool(r.get("Level")), to_bool(r.get("Sloping")),
            to_bool(r.get("Silt")), to_bool(r.get("Mud")), to_bool(r.get("Ooze")),
            to_bool(r.get("Detritus")), to_bool(r.get("Organic")),
            to_bool(r.get("HardBottom")), to_bool(r.get("Rocky")),
            to_bool(r.get("Rubble")), to_bool(r.get("Gravel")),
        ))

        # SpecialHabitat
        special_rows.append((
            auto, auto, to_str(r.get("SpecialHabitatRef")),
            to_bool(r.get("Macrophyte")), to_bool(r.get("BedsBivalve")), to_bool(r.get("BedsRock")),
            to_bool(r.get("SeaGrassBeds")), to_bool(r.get("BedsOthers")),
            to_bool(r.get("CoralReefs")), to_bool(r.get("ReefExclusive")),
            to_bool(r.get("DropOffs")), to_bool(r.get("ReefFlats")), to_bool(r.get("Lagoons")),
            to_bool(r.get("Burrows")), to_bool(r.get("Tunnels")), to_bool(r.get("Guyots")),
            to_bool(r.get("Crevices")), to_bool(r.get("Seamounts")),
            to_bool(r.get("ColdSeeps")), to_bool(r.get("HydrothermalVents")), to_bool(r.get("DeepWaterCorals")),
            to_bool(r.get("Vegetation")), to_bool(r.get("Leaves")),
            to_bool(r.get("Stems")), to_bool(r.get("Roots")), to_bool(r.get("Driftwood")),
            to_bool(r.get("OInverterbrates")), to_str(r.get("OIRemarks")),
            to_bool(r.get("Verterbrates")), to_str(r.get("VRemarks")),
            to_bool(r.get("Pilings")), to_bool(r.get("RicePaddies")), to_bool(r.get("BoatHulls")),
            to_bool(r.get("Corals")), to_bool(r.get("SoftCorals")),
            to_bool(r.get("OnPolyp")), to_bool(r.get("BetweenPolyps")),
            to_bool(r.get("HardCorals")), to_bool(r.get("OnExoskeleton")), to_bool(r.get("InterstitialSpaces")),
        ))

        # CircadianBehavior
        circadian_rows.append((
            auto, auto,
            to_str(r.get("Circadian1")), to_str(r.get("Circadian2")), to_str(r.get("Circadian3")),
            to_str(r.get("BioAspect1")), to_str(r.get("BioAspect2")), to_str(r.get("BioAspect3")),
            to_str(r.get("RemarksCircadian")),
            to_str(r.get("CircadianRef")), to_str(r.get("CircadianAlsoRef")),
        ))

    conn = connect()
    try:
        execute_upsert(conn, SQL_ECOLOGY, ecology_rows, "Ecologies")
        execute_upsert(conn, SQL_HABITAT, habitat_rows, "HabitatZones")
        execute_upsert(conn, SQL_FEEDING, feeding_rows, "FeedingAndDiets")
        execute_upsert(conn, SQL_ASSOC, assoc_rows, "Associations")
        execute_upsert(conn, SQL_SUBSTRATE, substrate_rows, "Substrates")
        execute_upsert(conn, SQL_SPECIAL, special_rows, "SpecialHabitats")
        execute_upsert(conn, SQL_CIRCADIAN, circadian_rows, "CircadianBehaviors")
    finally:
        conn.close()
