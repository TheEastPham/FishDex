"""
Chạy ETL CHỈ cho một tập SpecCode chỉ định — dùng khi bổ sung loài mới.

    python run_scoped.py <file_ma.txt>

VÌ SAO KHÔNG DÙNG `python -m etl.run`
─────────────────────────────────────
`run.py` tính spec_codes = bộ lọc Aquarium ∪ new_spec_codes.txt, rồi truyền
**cả tập** cho mọi loader. Mà `images.py` làm:

    DELETE FROM "SystemImages" WHERE "SpecCode" IN (...)
    INSERT ... VALUES (uuid.uuid4(), ...)

tức **sinh GUID mới**. `SystemImage.ObjectKey` là `{SpecCode}/{Id}{ext}`, còn file
trên R2 mang GUID **cũ** → chạy đầy đủ là đứt toàn bộ ảnh đang hiển thị.

Nguy hơn: local và PROD **dùng chung một bucket R2**, nên chạy nhầm ở local là
hỏng ảnh PROD ngay, không có bước duyệt nào chen vào giữa.

Script này chỉ truyền **mã mới tinh** cho các loader có tham số. Mã mới thì không
có dòng nào để xoá, nên GUID của loài cũ không ai đụng.

Ba loader nạp toàn bộ và KHÔNG nhận mã (families, genera, ecosystemref) vẫn chạy
bình thường — chúng upsert nên vô hại. `species_index` chạy cuối để tính lại
cột IsLoaded trên toàn bảng.
"""
from __future__ import annotations
import sys
import time
from pathlib import Path

from etl import audit
from etl.db import connect
from etl.loaders import (
    families, genera, species, stocks, ecology, morph,
    ecosystem, occurrence, common_names, images, species_index,
)

# Giữ đúng thứ tự của etl/run.py — species phải xong trước stocks/ecology,
# species_index phải chạy cuối cùng.
BUOC = [
    ("families",     "Families",             lambda c: families.load()),
    ("genera",       "Genuses",              lambda c: genera.load()),
    ("species",      "Species",              lambda c: species.load(c)),
    ("stocks",       "Stocks+children",      lambda c: stocks.load(c)),
    ("ecology",      "Ecology+children",     lambda c: ecology.load(c)),
    ("morph",        "MorphData+children",   lambda c: morph.load(c)),
    ("ecosystemref", "EcosystemRefs",        lambda c: ecosystem.load_refs()),
    ("ecosystem",    "Ecosystems",           lambda c: ecosystem.load_junction(c)),
    ("occurrence",   "Occurrences",          lambda c: occurrence.load(c)),
    ("comnames",     "CommonNames",          lambda c: common_names.load(c)),
    ("images",       "SystemImages",         lambda c: images.load(c)),
    ("species_index","FishBaseSpeciesIndex", lambda c: species_index.load()),
]


def doc_ma(path: Path) -> set[int]:
    """Đọc file mã: phẩy / khoảng trắng / xuống dòng đều được, bỏ dòng chú thích."""
    raw = "\n".join(
        l for l in path.read_text(encoding="utf-8").splitlines()
        if not l.strip().startswith("#")
    )
    return {
        int(t) for t in raw.replace("\n", ",").replace(" ", ",").split(",")
        if t.strip().isdigit()
    }


def main() -> int:
    if len(sys.argv) < 2:
        print(__doc__)
        return 1

    f = Path(sys.argv[1])
    if not f.exists():
        print(f"Khong tim thay {f}", file=sys.stderr)
        return 1

    codes = doc_ma(f)
    if not codes:
        print("File khong co ma nao", file=sys.stderr)
        return 1

    print(f"Nap {len(codes):,} SpecCode tu {f.name}")
    print(f"  vi du: {sorted(codes)[:8]}")
    print()

    for i, (ten, mo_ta, fn) in enumerate(BUOC, start=1):
        t0 = time.time()
        print(f"[{i:>2}/{len(BUOC)}] {ten:<14} {mo_ta}")
        fn(codes)
        print(f"          xong sau {time.time()-t0:.1f}s\n")

    print("HOAN TAT.")

    # Hậu kiểm — quan trọng gấp đôi ở đây: lô loài mới chỉ có ĐÚNG MỘT LẦN để nạp
    # cho đúng, vì ETL không bao giờ được chạy lại cho loài đã có (GUID ảnh).
    # Nạp hỏng mà không phát hiện ngay thì phải đi đường vá tay bằng build_delta.py.
    conn = connect()
    try:
        audit.check(conn)
    finally:
        conn.close()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
