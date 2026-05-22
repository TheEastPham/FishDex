"""
ETL entry point. Chạy từ thư mục Pipeline/FishDexLocal/:

    python -m etl.run                # chạy hết
    python -m etl.run --only 3       # chỉ chạy step 3 (species)
    python -m etl.run --from 5       # chạy từ step 5 trở đi
    python -m etl.run --steps 1,2,3  # chạy step 1, 2, 3
    python -m etl.run --dry-run      # chỉ tính spec_codes và in stats

Step order (FK dependency):
    1.  families      → "Families"
    2.  genera        → "Genuses"
    3.  species       → "Species"            *** filter spec_codes ***
    4.  stocks        → "Stocks" + Conservation + Environment + ExternalRef + Metadata
    5.  ecology       → "Ecologies" + HabitatZone + FeedingAndDiet + Associations + Substrate + SpecialHabitat + CircadianBehavior
    6.  morphdat      → "MorphData" + Teeth + Pigmentation + Fins + Meristics + Metrics
    7.  ecosystemref  → "EcosystemRefs" (load TOÀN BỘ, không filter)
    8.  ecosystem     → "Ecosystems" (junction, filter spec_codes)
    9.  occurrence    → "Occurrences"
    10. comnames      → "CommonNames"
    11. images        → "SystemImages" (optional)
"""
from __future__ import annotations
import argparse
import time
from .filter import compute_spec_codes
from .loaders import (
    families, genera, species, stocks, ecology, morph,
    ecosystem, occurrence, common_names, images,
)


STEPS = [
    ("families",     "Families",        lambda codes: families.load()),
    ("genera",       "Genuses",         lambda codes: genera.load()),
    ("species",      "Species",         lambda codes: species.load(codes)),
    ("stocks",       "Stocks+children", lambda codes: stocks.load(codes)),
    ("ecology",      "Ecology+children",lambda codes: ecology.load(codes)),
    ("morph",        "MorphData+children", lambda codes: morph.load(codes)),
    ("ecosystemref", "EcosystemRefs",   lambda codes: ecosystem.load_refs()),
    ("ecosystem",    "Ecosystems",      lambda codes: ecosystem.load_junction(codes)),
    ("occurrence",   "Occurrences",     lambda codes: occurrence.load(codes)),
    ("comnames",     "CommonNames",     lambda codes: common_names.load(codes)),
    ("images",       "SystemImages",    lambda codes: images.load(codes)),
]


def _parse_steps(only: str | None, frm: int | None, steps: str | None) -> list[int]:
    n = len(STEPS)
    if only is not None:
        return [only]
    if frm is not None:
        return list(range(frm, n + 1))
    if steps is not None:
        return [int(s.strip()) for s in steps.split(",") if s.strip()]
    return list(range(1, n + 1))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--only", type=int, help="Chỉ chạy 1 step (1-11)")
    ap.add_argument("--from", dest="frm", type=int, help="Chạy từ step N trở đi")
    ap.add_argument("--steps", type=str, help="Chạy các step nhất định, vd: 1,2,3")
    ap.add_argument("--dry-run", action="store_true", help="Chỉ tính spec_codes")
    args = ap.parse_args()

    print("=" * 70)
    print("FishDex ETL — FishBase parquet → PostgreSQL")
    print("=" * 70)

    print("\n[Step 0] Compute spec_codes ...")
    spec_codes = compute_spec_codes()

    if args.dry_run:
        print("\n[DRY RUN] Stopping after spec_codes computation.")
        return

    selected = _parse_steps(args.only, args.frm, args.steps)
    print(f"\nWill run steps: {selected}\n")

    total_start = time.time()
    for idx in selected:
        if idx < 1 or idx > len(STEPS):
            print(f"[!] Step {idx} out of range, skipped.")
            continue
        name, label, fn = STEPS[idx - 1]
        print(f"\n[Step {idx}] {label} ({name}) ...")
        t = time.time()
        try:
            fn(spec_codes)
        except Exception as e:
            print(f"  [ERROR] Step {idx} failed: {e!r}")
            raise
        print(f"  ⏱ {time.time() - t:.1f}s")

    print(f"\n{'=' * 70}")
    print(f"Done in {time.time() - total_start:.1f}s")
    print(f"{'=' * 70}")
    print("\n💡 Sau khi ETL xong, reset sequences (nếu có INSERT với explicit PK):")
    print("   psql -h localhost -p 5433 -U fishdex -d fishdex -f post_etl.sql")


if __name__ == "__main__":
    main()
