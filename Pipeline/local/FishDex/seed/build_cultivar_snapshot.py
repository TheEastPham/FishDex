"""
Sinh file nạp DB cho registry dòng lai.

    python seed/build_cultivar_snapshot.py

Đọc `cultivars.csv` (file người curate) và xuất `snapshot_insert.csv` — đúng cột của
bảng `SpeciesSnapshots`. Hai file tách nhau có chủ đích: file curate mang mô tả tiếng
Việt, cột `canDuyet` và cột `nguon`, không thứ nào vào DB.

CHỈ ĐỌC `cultivars.csv`. Output không commit.

── SpecCode phải đóng băng trong file curate, KHÔNG để DB tự cấp ────────────────
Luồng community cấp mã bằng MAX+1. Nếu local cấp Ranchu là 500003 mà PROD cấp
500017 thì `TradedSpecies` của Việt Nam trỏ 500003 sẽ TRỎ SAI LOÀI trên PROD.
Registry gọi là toàn cục thì mã phải giống nhau ở mọi môi trường.

Mã sống ở `cultivar-codes.csv`, **khoá theo cột `khoa` chứ không theo thứ tự dòng** —
nhờ vậy sắp lại `cultivars.csv` không làm lệch mã. File đó append-only.

Script **từ chối chạy** nếu có dòng lai chưa có mã — thà dừng còn hơn tự bịa mã rồi
phá dữ liệu market của mọi quốc gia.

Dải dùng: 500001+. **KHÔNG BAO GIỜ đánh số lại.** Xoá một dòng lai thì để trống mã
đó, đừng dồn lên.
"""
from __future__ import annotations
import csv
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
CURATED = HERE / "cultivars.csv"
CODES = HERE / "cultivar-codes.csv"
OUT = HERE / "snapshot_insert.csv"

# Giá trị enum, đã đối chiếu code BE — tiện là đều bằng 1
WATER_TYPE_FRESHWATER = 1   # FishLover.Shared.Common.Enum.WaterType
DATA_SOURCE_COMMUNITY = 1   # SnapshotDataSource
POPULATED_FROM_MANUAL = 1   # SnapshotPopulatedFrom
KIND_HYBRID = 1             # CommunitySpeciesKind — owner chốt dùng Hybrid cho cả
                            # dòng lai tạo, không thêm giá trị enum mới

FIELDS = [
    "SpecCode", "SpeciesName", "CommonName", "GenusName",
    "WaterType", "Kind", "DataSource", "PopulatedFrom", "IsVerified",
    "ParentSpecCode",
]


def main() -> int:
    if not CURATED.exists():
        print(f"Khong tim thay {CURATED}", file=sys.stderr)
        return 1

    with CURATED.open(encoding="utf-8-sig", newline="") as f:
        rows = list(csv.DictReader(f))

    if not rows:
        print("cultivars.csv rong", file=sys.stderr)
        return 1

    if not CODES.exists():
        print(f"Khong tim thay {CODES} — chua co bang ma", file=sys.stderr)
        return 1

    # Bảng mã khoá theo `khoa`. Bỏ dòng chú thích bắt đầu bằng #
    with CODES.open(encoding="utf-8-sig", newline="") as f:
        ma = {
            r["khoa"]: r["specCode"].strip()
            for r in csv.DictReader(l for l in f if not l.startswith("#"))
        }

    thieu = [r["khoa"] for r in rows if not ma.get(r["khoa"])]
    if thieu:
        print(f"{len(thieu)} dong lai CHUA CO MA trong cultivar-codes.csv:", file=sys.stderr)
        for t in thieu[:10]:
            print(f"  - {t}", file=sys.stderr)
        print("Them ma moi vao CUOI file do, dung danh so lai.", file=sys.stderr)
        return 1

    # Trùng mã là lỗi chết người: hai dòng lai cùng mã thì market của mọi nước lệch
    dung = [ma[r["khoa"]] for r in rows]
    trung = {c for c in dung if dung.count(c) > 1}
    if trung:
        print(f"specCode TRUNG: {sorted(trung)}", file=sys.stderr)
        return 1

    out = []
    for r in rows:
        parent = (r.get("loaiChaTen") or "").strip()
        name = (r.get("tenQuocTe") or "").strip()

        # Quy ước cultivar quốc tế: Chi loài 'Tên dòng' — sắp cạnh loài cha khi sort,
        # và quan hệ cha-con hiện ngay trong tên. Dòng lai khác loài không có loài cha
        # hợp lệ thì để trống phần trước dấu nháy.
        species_name = f"{parent} '{name}'" if parent else name

        out.append({
            "SpecCode":       ma[r["khoa"]],
            "SpeciesName":    species_name,
            "CommonName":     name,
            "GenusName":      parent.split()[0] if parent else "",
            "WaterType":      WATER_TYPE_FRESHWATER,
            "Kind":           KIND_HYBRID,
            "DataSource":     DATA_SOURCE_COMMUNITY,
            "PopulatedFrom":  POPULATED_FROM_MANUAL,
            "IsVerified":     "true",
            # KHÔNG phải cột của SpeciesSnapshots — mang theo để câu SQL nạp join
            # sang Species lấy FamilyName thật. Xem ghi chú cuối file.
            "ParentSpecCode": (r.get("loaiChaSpecCode") or "").strip(),
        })

    with OUT.open("w", newline="", encoding="utf-8") as f:
        w = csv.DictWriter(f, fieldnames=FIELDS)
        w.writeheader()
        w.writerows(out)

    co_cha = sum(1 for r in out if r["ParentSpecCode"])
    print(f"  Da ghi {OUT}")
    print(f"    {len(out)} dong lai, ma {out[0]['SpecCode']} -> {out[-1]['SpecCode']}")
    print(f"    {co_cha} dong co loai cha, {len(out) - co_cha} dong lai khac loai khong co cha")
    print()
    print("  Buoc tiep: cau SQL upsert doc file nay, va tu dien FamilyName bang")
    print("  join sang Species theo ParentSpecCode. PopulatedAt de SQL dat now().")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
