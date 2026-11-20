"""
Tính toán spec_codes cần load: Fresh=1 + Aquarium in AQUARIUM_VALUES,
UNION thêm các code chỉ định trong new_spec_codes.txt (bỏ qua filter).
"""
from __future__ import annotations
import polars as pl
from .config import (
    PARQUET_DIR, PARQUET_FILES, AQUARIUM_VALUES, INCLUDE_BRACKISH, NEW_SPEC_CODES_FILE,
)


def read_explicit_codes() -> set[int]:
    """Đọc new_spec_codes.txt — các SpecCode chỉ định thêm, bỏ qua Aquarium filter.
    Ngăn cách bởi dấu phẩy / khoảng trắng / xuống dòng. File không tồn tại → set rỗng."""
    if not NEW_SPEC_CODES_FILE.exists():
        return set()

    raw = NEW_SPEC_CODES_FILE.read_text(encoding="utf-8")
    codes: set[int] = set()
    for token in raw.replace("\n", ",").replace(" ", ",").split(","):
        token = token.strip()
        if token.isdigit():
            codes.add(int(token))
    return codes


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

    # UNION explicit codes — loài yêu cầu thêm dù không pass filter
    explicit = read_explicit_codes()
    if explicit:
        new_beyond = explicit - codes
        codes |= explicit
        print(
            f"  [explicit] +{len(explicit):,} code từ new_spec_codes.txt"
            f" ({len(new_beyond):,} nằm ngoài filter) → tổng {len(codes):,}"
        )

    return codes
