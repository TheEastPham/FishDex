"""
Tính toán spec_codes cần load: Fresh=1 + Aquarium in AQUARIUM_VALUES.
"""
from __future__ import annotations
import polars as pl
from .config import PARQUET_DIR, PARQUET_FILES, AQUARIUM_VALUES, INCLUDE_BRACKISH


def compute_spec_codes() -> set[int]:
    path = PARQUET_DIR / PARQUET_FILES["species"]
    if not path.exists():
        raise FileNotFoundError(f"Không tìm thấy {path}")

    df = pl.read_parquet(
        path,
        columns=["SpecCode", "Fresh", "Brack", "Aquarium"],
    )

    # Freshwater mask
    mask = pl.col("Fresh") == 1
    if INCLUDE_BRACKISH:
        mask = mask | (pl.col("Brack") == 1)

    # Aquarium filter
    aquarium_list = list(AQUARIUM_VALUES)
    filtered = df.filter(mask & pl.col("Aquarium").is_in(aquarium_list))

    codes = set(filtered["SpecCode"].to_list())
    print(
        f"  [filter] spec_codes: {len(codes):,} loài"
        f" (từ tổng {len(df):,}, Fresh={df.filter(pl.col('Fresh')==1).height:,})"
    )
    return codes
