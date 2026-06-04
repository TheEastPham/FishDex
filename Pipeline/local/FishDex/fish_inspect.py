"""
Step 0 (chạy TRƯỚC khi ETL): kiểm tra các giá trị thực tế trong parquet.

Mục đích:
- Xem các giá trị thực tế của Aquarium field
- Đếm số loài Fresh / Brack / Saltwater
- Xem preview các loài sẽ được lấy với filter hiện tại

Cách chạy:
    cd Pipeline/FishDexLocal
    python inspect.py
"""
from __future__ import annotations
import polars as pl
from pathlib import Path

PARQUET_DIR = Path(__file__).parent / "parquetData"


def main():
    path = PARQUET_DIR / "species.parquet"
    if not path.exists():
        print(f"[ERROR] Không thấy {path}")
        print("        Bỏ file species.parquet vào parquetData/ rồi chạy lại.")
        return

    print(f"[*] Doc {path.name} ...")
    df = pl.read_parquet(
        path,
        columns=[
            "SpecCode", "Genus", "Species", "FBname",
            "Fresh", "Brack", "Saltwater",
            "Aquarium", "AquariumFishII",
        ],
    )

    total = len(df)
    fresh_n = df.filter(pl.col("Fresh") == 1).height
    brack_n = df.filter(pl.col("Brack") == 1).height
    salt_n = df.filter(pl.col("Saltwater") == 1).height

    print(f"\n{'=' * 60}")
    print(f"Tổng số loài     : {total:,}")
    print(f"Fresh = 1        : {fresh_n:,}")
    print(f"Brack = 1        : {brack_n:,}")
    print(f"Saltwater = 1    : {salt_n:,}")
    print(f"{'=' * 60}")

    print("\n=== Giá trị thực tế của Aquarium (Fresh=1) ===")
    fresh = df.filter(pl.col("Fresh") == 1)
    aq_counts = (
        fresh.group_by("Aquarium")
        .agg(pl.len().alias("count"))
        .sort("count", descending=True)
    )
    print(aq_counts)

    print("\n=== Giá trị thực tế của AquariumFishII (Fresh=1) ===")
    aq2_counts = (
        fresh.group_by("AquariumFishII")
        .agg(pl.len().alias("count"))
        .sort("count", descending=True)
    )
    print(aq2_counts)

    # Preview với recommended filter
    recommended = ["highly commercial", "commercial"]
    matched = fresh.filter(pl.col("Aquarium").is_in(recommended))
    print(f"\n=== Với filter Fresh=1 + Aquarium ∈ {recommended} ===")
    print(f"Số loài match: {matched.height:,}")

    print("\nPreview 20 loài đầu:")
    print(matched.select(["SpecCode", "Genus", "Species", "FBname", "Aquarium"]).head(20))

    # Test thêm với 'of minor importance'
    looser = ["highly commercial", "commercial", "of minor importance"]
    matched2 = fresh.filter(pl.col("Aquarium").is_in(looser))
    print(f"\n=== Nếu lấy thêm 'of minor importance' ===")
    print(f"Số loài match: {matched2.height:,}")

    print("\n[!] Sau khi kiem tra:")
    print("   - Edit etl/config.py → cập nhật AQUARIUM_VALUES")
    print("   - Chạy: python -m etl.run --dry-run  để xác nhận spec_codes")
    print("   - Chạy: python -m etl.run            để load thật")


if __name__ == "__main__":
    main()
