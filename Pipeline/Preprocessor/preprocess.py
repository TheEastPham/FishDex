#!/usr/bin/env python3
"""
FishBase parquet -> JSONL chunks for RAG pipeline.

Filter: species kept in aquariums (commercial/highly commercial/...)
        OR species that have an English common name.

Output: output/species_chunks.jsonl
Each line: {"specCode": int, "chunkIndex": int, "text": str}
"""

import json
import math
from pathlib import Path

import duckdb
import tiktoken
from tqdm import tqdm

PARQUET_DIR = Path(__file__).parent.parent / "FishDexLocal" / "parquetData"
OUTPUT_DIR  = Path(__file__).parent / "output"
OUTPUT_FILE = OUTPUT_DIR / "species_chunks.jsonl"

MAX_TOKENS = 500
ENCODING   = tiktoken.get_encoding("cl100k_base")

AQUARIUM_VALUES = (
    "'commercial'", "'highly commercial'",
    "'public aquariums'", "'show aquarium'", "'potential'"
)


def _str(val) -> str:
    """Return string value, treating NaN/None as empty string."""
    if val is None:
        return ""
    if isinstance(val, float) and math.isnan(val):
        return ""
    return str(val).strip()


def _int(val) -> int | None:
    """Return int value, treating NaN/None as None."""
    if val is None:
        return None
    if isinstance(val, float) and math.isnan(val):
        return None
    try:
        return int(val)
    except (ValueError, TypeError):
        return None


def build_text(row: dict) -> str:
    parts = []

    # ── Identity ────────────────────────────────────────────────
    sci  = f"{_str(row.get('Genus'))} {_str(row.get('Species'))}"
    line = f"Species: {sci}"
    if _str(row.get("Author")):
        line += f" ({_str(row['Author'])})"
    parts.append(line)

    if _str(row.get("common_name")):
        parts.append(f"Common name: {_str(row['common_name'])}")

    # ── Taxonomy ────────────────────────────────────────────────
    tax = []
    if _str(row.get("Family")): tax.append(f"Family: {_str(row['Family'])}")
    if _str(row.get("FOrder")): tax.append(f"Order: {_str(row['FOrder'])}")
    if _str(row.get("Class")):  tax.append(f"Class: {_str(row['Class'])}")
    if tax:
        parts.append(" | ".join(tax))

    # ── Water type ──────────────────────────────────────────────
    water = []
    if _int(row.get("Fresh"))     == 1: water.append("freshwater")
    if _int(row.get("Brack"))     == 1: water.append("brackish")
    if _int(row.get("Saltwater")) == 1: water.append("saltwater")
    if water:
        parts.append(f"Water type: {', '.join(water)}")

    if _str(row.get("DemersPelag")):
        parts.append(f"Position in water: {_str(row['DemersPelag'])}")

    # ── Size & lifespan ─────────────────────────────────────────
    size = []
    if row.get("Length") and not (isinstance(row["Length"], float) and math.isnan(row["Length"])):
        size.append(f"max length {row['Length']} cm")
    if row.get("Weight") and not (isinstance(row["Weight"], float) and math.isnan(row["Weight"])):
        size.append(f"max weight {row['Weight']} g")
    if size:
        parts.append(f"Size: {', '.join(size)}")

    longevity = row.get("LongevityCaptive")
    if longevity and not (isinstance(longevity, float) and math.isnan(longevity)):
        parts.append(f"Lifespan in captivity: {longevity} years")

    # ── Aquarium trade ──────────────────────────────────────────
    if _str(row.get("Aquarium")):
        parts.append(f"Aquarium trade: {_str(row['Aquarium'])}")

    # ── Water parameters ────────────────────────────────────────
    tmin = row.get("TempMin")
    tmax = row.get("TempMax")
    tpref = row.get("TempPreferred")
    temp = []
    if tmin is not None and tmax is not None \
            and not (isinstance(tmin, float) and math.isnan(tmin)) \
            and not (isinstance(tmax, float) and math.isnan(tmax)):
        temp.append(f"{tmin}-{tmax}°C")
    elif tpref is not None and not (isinstance(tpref, float) and math.isnan(tpref)):
        temp.append(f"preferred {tpref}°C")
    if temp:
        parts.append(f"Temperature: {', '.join(temp)}")

    if _str(row.get("pHOpt")):
        parts.append(f"pH optimal: {_str(row['pHOpt'])}")

    # ── Ecology ─────────────────────────────────────────────────
    if _str(row.get("FeedingType")):
        parts.append(f"Feeding: {_str(row['FeedingType'])}")

    behavior = []
    if _int(row.get("Schooling")) == 1: behavior.append("schooling")
    if _int(row.get("Shoaling"))  == 1: behavior.append("shoaling")
    if _int(row.get("Solitary"))  == 1: behavior.append("solitary")
    if behavior:
        parts.append(f"Behavior: {', '.join(behavior)}")

    # ── Danger ──────────────────────────────────────────────────
    dangerous = _str(row.get("Dangerous"))
    if dangerous.lower() not in ("", "harmless", "none", "reports of harm doubtful"):
        parts.append(f"Dangerous: {dangerous}")

    # ── Free-text notes (truncated) ─────────────────────────────
    comments = _str(row.get("Comments"))
    if comments:
        parts.append(f"Notes: {comments[:400]}")

    return "\n".join(parts)


def chunk_text(text: str, spec_code: int) -> list[dict]:
    tokens = ENCODING.encode(text)
    if len(tokens) <= MAX_TOKENS:
        return [{"specCode": spec_code, "chunkIndex": 0, "text": text}]

    # Overflow: split at line boundaries
    chunks, buf, buf_tokens, idx = [], [], 0, 0
    for line in text.split("\n"):
        lt = len(ENCODING.encode(line + "\n"))
        if buf_tokens + lt > MAX_TOKENS and buf:
            chunks.append({"specCode": spec_code, "chunkIndex": idx,
                           "text": "\n".join(buf)})
            idx += 1
            buf, buf_tokens = [], 0
        buf.append(line)
        buf_tokens += lt
    if buf:
        chunks.append({"specCode": spec_code, "chunkIndex": idx,
                       "text": "\n".join(buf)})
    return chunks


def main():
    OUTPUT_DIR.mkdir(exist_ok=True)
    con = duckdb.connect()

    aq_filter = ", ".join(AQUARIUM_VALUES)

    print("Querying and joining parquet tables (this may take ~30s)...")
    df = con.execute(f"""
        SELECT
            s.SpecCode,
            s.Genus,
            s.Species,
            s.Author,
            s.Fresh,
            s.Brack,
            s.Saltwater,
            s.DemersPelag,
            s.Length,
            s.Weight,
            s.LongevityCaptive,
            s.Aquarium,
            s.Dangerous,
            s.Comments,
            f.Family,
            f."Order"       AS FOrder,
            f.Class,
            cn.ComName      AS common_name,
            st.TempMin,
            st.TempMax,
            st.TempPreferred,
            wq.pHOpt,
            ec.FeedingType,
            ec.Schooling,
            ec.Shoaling,
            ec.Solitary
        FROM read_parquet('{PARQUET_DIR}/species.parquet') s

        LEFT JOIN read_parquet('{PARQUET_DIR}/families.parquet') f
            ON s.FamCode = f.FamCode

        -- one preferred English name per species
        LEFT JOIN (
            SELECT SpecCode, ComName
            FROM read_parquet('{PARQUET_DIR}/comnames.parquet')
            WHERE Language = 'English' AND PreferredName = 1
            QUALIFY ROW_NUMBER() OVER (
                PARTITION BY SpecCode ORDER BY Rank ASC NULLS LAST
            ) = 1
        ) cn ON s.SpecCode = cn.SpecCode

        -- first stock temperature data per species
        LEFT JOIN (
            SELECT SpecCode, TempMin, TempMax, TempPreferred
            FROM read_parquet('{PARQUET_DIR}/stocks.parquet')
            WHERE TempMin IS NOT NULL OR TempMax IS NOT NULL
            QUALIFY ROW_NUMBER() OVER (PARTITION BY SpecCode ORDER BY StockCode ASC) = 1
        ) st ON s.SpecCode = st.SpecCode

        -- first water quality record per species
        LEFT JOIN (
            SELECT Speccode AS SpecCode, pHOpt
            FROM read_parquet('{PARQUET_DIR}/waterquality.parquet')
            WHERE pHOpt IS NOT NULL AND pHOpt != ''
            QUALIFY ROW_NUMBER() OVER (PARTITION BY Speccode ORDER BY autoctr ASC) = 1
        ) wq ON s.SpecCode = wq.SpecCode

        -- first ecology record per species
        LEFT JOIN (
            SELECT SpecCode, FeedingType, Schooling, Shoaling, Solitary
            FROM read_parquet('{PARQUET_DIR}/ecology.parquet')
            QUALIFY ROW_NUMBER() OVER (PARTITION BY SpecCode ORDER BY autoctr ASC) = 1
        ) ec ON s.SpecCode = ec.SpecCode

        WHERE s.Aquarium IN ({aq_filter})
           OR cn.ComName IS NOT NULL

        ORDER BY s.SpecCode
    """).df()

    print(f"Loaded {len(df):,} species. Building chunks...")

    total_chunks = 0
    token_counts = []

    with open(OUTPUT_FILE, "w", encoding="utf-8") as f:
        for _, row in tqdm(df.iterrows(), total=len(df)):
            text   = build_text(row.to_dict())
            chunks = chunk_text(text, int(row["SpecCode"]))
            for c in chunks:
                f.write(json.dumps(c, ensure_ascii=False) + "\n")
                token_counts.append(len(ENCODING.encode(c["text"])))
            total_chunks += len(chunks)

    avg = int(sum(token_counts) / len(token_counts)) if token_counts else 0
    print(f"\nDone!")
    print(f"  Species:  {len(df):,}")
    print(f"  Chunks:   {total_chunks:,}")
    print(f"  Tokens/chunk: min={min(token_counts)}, avg={avg}, max={max(token_counts)}")
    print(f"  Output:   {OUTPUT_FILE}")


if __name__ == "__main__":
    main()
