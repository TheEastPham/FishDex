"""
Sinh danh sách ứng viên cho lớp market Việt Nam.

    python seed/build_market_seed.py

Lấy tên khoa học trong `market-vn.csv` (curate tay từ 4 tiệm cá thật) đem TRA trong
`species.parquet` của FishBase để biết SpecCode, rồi ghi kết quả ra CSV mới.

CHỈ ĐỌC parquet. Script không bao giờ ghi vào `parquetData/` — mọi output đều nằm
trong chính thư mục `seed/`. Sửa file này thì phải giữ nguyên tính chất đó:
parquet là dữ liệu gốc của ETL, hỏng là phải tải lại toàn bộ.

Đọc README.md trước khi sửa file này — nhất là luật tách loài lai tạo và lý do
không được tin trường `Aquarium` của FishBase.

Ba tầng tín hiệu, tier càng nhỏ càng chắc:

  tier 1 — tiệm Việt Nam bán thật (mọi dòng `loai` có tenKhoaHoc trong market-vn.csv)
  tier 2 — FishBase `Aquarium = 'highly commercial'` (tín hiệu TOÀN CẦU, không riêng VN)
  tier 3 — FishBase `Aquarium = 'commercial'` + loài có tên tiếng Việt trong comnames

Chỉ tier 1 là bằng chứng thị trường Việt Nam. Tier 2 và 3 chỉ là điểm khởi đầu
cho admin, đừng nhầm chúng với khảo sát thị trường.

Tiệm tự ghi tên khoa học hay người curate tra ra thì đều vào tier 1 — phân biệt
đó nằm ở cột `ghiChu` của market-vn.csv cho người đọc, máy không tách được nữa.
"""
from __future__ import annotations
import csv
import sys
from pathlib import Path

import pyarrow.parquet as pq

HERE = Path(__file__).resolve().parent
PARQUET = HERE.parent / "parquetData"  # chỉ đọc, không bao giờ ghi vào đây

CURATED = HERE / "market-vn.csv"
OUT = HERE / "market_vn.csv"
CULTIVAR_OUT = HERE / "market_vn_cultivars.csv"
TODO = HERE / "market_vn_todo.csv"

# Phải khớp AQUARIUM_VALUES trong etl/config.py, nếu không seed sẽ chứa loài không có trong DB
AQUARIUM_IN_DB = {"highly commercial", "commercial", "potential"}

COUNTRY_ALPHA2 = "VN"
COUNTRY_LANGUAGE = "Vietnamese"


def is_fresh(v) -> bool:
    # FishBase dùng -1 cho true ở nhiều cột boolean
    return v in (1, True, -1)


def read_curated() -> tuple[dict[str, str], list[dict], list[dict]]:
    """
    Đọc market-vn.csv, tách theo cột `loai`.

    Trả về (tên khoa học viết thường -> tên bán, dòng lai tạo, dòng chưa tra được).
    """
    species: dict[str, str] = {}
    cultivars: list[dict] = []
    unresolved: list[dict] = []

    with CURATED.open(encoding="utf-8-sig", newline="") as f:
        for row in csv.DictReader(f):
            kind = (row.get("loai") or "").strip()
            name = (row.get("tenBan") or "").strip()
            if not name:
                continue

            if kind == "loai":
                sci = (row.get("tenKhoaHoc") or "").strip()
                if sci:
                    # Trùng tên khoa học thì giữ dòng đầu — vd chuột Venezuela và chuột Albino
                    species.setdefault(sci.lower(), name)
                else:
                    unresolved.append(row)
            elif kind == "laiTao":
                cultivars.append(row)
            else:
                unresolved.append(row)

    return species, cultivars, unresolved


def main() -> int:
    if not PARQUET.exists():
        print(f"Khong tim thay {PARQUET}", file=sys.stderr)
        return 1
    if not CURATED.exists():
        print(f"Khong tim thay {CURATED}", file=sys.stderr)
        return 1

    curated, cultivars, unresolved = read_curated()

    species = pq.read_table(
        PARQUET / "species.parquet",
        columns=["SpecCode", "Genus", "Species", "Aquarium", "Fresh", "FBname"],
    ).to_pylist()

    comnames = pq.read_table(
        PARQUET / "comnames.parquet", columns=["SpecCode", "Language"]
    ).to_pylist()

    has_local_name = {
        r["SpecCode"] for r in comnames
        if str(r["Language"]).strip() == COUNTRY_LANGUAGE
    }

    rows = []
    not_in_fishdex: list[tuple[str, str, str]] = []

    for r in species:
        aquarium = (str(r["Aquarium"]).strip() if r["Aquarium"] else "")
        spec_code = r["SpecCode"]
        name = f"{(r['Genus'] or '').strip()} {(r['Species'] or '').strip()}".strip()

        shop_name = curated.pop(name.lower(), None)

        if not (is_fresh(r["Fresh"]) and aquarium in AQUARIUM_IN_DB):
            # Tiệm bán thật nhưng bộ lọc AQUARIUM_VALUES của ETL không nạp loài này.
            # Không được lặng lẽ bỏ: seed vào sẽ ra dòng trống vì Species không có bản ghi.
            if shop_name:
                why = ("khong phai nuoc ngot" if not is_fresh(r["Fresh"])
                       else f"Aquarium='{aquarium or 'trong'}'")
                not_in_fishdex.append((name, why, shop_name))
            continue

        if shop_name:
            tier, trade_status = 1, "Common"
            reason = f"tiem VN ban: {shop_name}"
        elif aquarium == "highly commercial":
            tier, trade_status = 2, "Common"
            reason = "FishBase Aquarium='highly commercial' (tin hieu toan cau)"
        elif spec_code in has_local_name and aquarium in ("commercial", "potential"):
            tier, trade_status = 3, ""
            reason = f"FishBase Aquarium='{aquarium}' + co ten {COUNTRY_LANGUAGE}"
        else:
            continue

        rows.append({
            "specCode": spec_code,
            "scientificName": name,
            "fbName": (r["FBname"] or "").strip(),
            "tradeStatus": trade_status,
            "tier": tier,
            "reason": reason,
        })

    rows.sort(key=lambda x: (x["tier"], x["scientificName"]))

    with OUT.open("w", newline="", encoding="utf-8") as f:
        w = csv.DictWriter(f, fieldnames=["specCode", "scientificName", "fbName",
                                          "tradeStatus", "tier", "reason"])
        w.writeheader()
        w.writerows(rows)

    # Loài lai tạo chưa có SpecCode nên không vào được market_vn.csv. Phải submit qua
    # luồng community species (SpeciesSnapshot, DataSource=Community) để được cấp mã
    # >= 500000, rồi mới thêm mã đó vào lớp market.
    with CULTIVAR_OUT.open("w", newline="", encoding="utf-8") as f:
        w = csv.writer(f)
        w.writerow(["tenLaiTao", "loaiCha", "nguon", "ghiChu"])
        for c in sorted(cultivars, key=lambda x: x["tenBan"]):
            w.writerow([c["tenBan"], c.get("loaiCha", ""), c.get("nguon", ""), c.get("ghiChu", "")])

    # Việc còn lại cho người curate. Ghi ra file để lần sau khỏi phải dò lại web.
    with TODO.open("w", newline="", encoding="utf-8") as f:
        w = csv.writer(f)
        w.writerow(["loaiViec", "ten", "nguon", "ghiChu"])
        for u in sorted(unresolved, key=lambda x: x["tenBan"]):
            w.writerow(["chua_tra_duoc_ten_khoa_hoc", u["tenBan"],
                        u.get("nguon", ""), u.get("ghiChu", "")])
        for sci, shop_name in sorted(curated.items()):
            w.writerow(["ten_khoa_hoc_khong_co_trong_fishbase", sci, "",
                        f"sai chinh ta hoac ten dong nghia | ban duoi ten '{shop_name}'"])
        for name, why, shop_name in sorted(not_in_fishdex):
            w.writerow(["tiem_ban_nhung_ETL_khong_nap", name, "", f"{why} | {shop_name}"])

    counts = {t: sum(1 for r in rows if r["tier"] == t) for t in (1, 2, 3)}
    print(f"  Da ghi {OUT}")
    print(f"    tier 1 (tiem VN ban that):                {counts[1]}")
    print(f"    tier 2 (FishBase highly commercial):      {counts[2]}")
    print(f"    tier 3 (commercial + ten {COUNTRY_LANGUAGE}):    {counts[3]}")
    print(f"    TONG {len(rows)} loai ung vien cho {COUNTRY_ALPHA2}")
    print()
    print(f"  Da ghi {CULTIVAR_OUT}: {len(cultivars)} loai lai tao")
    print( "    phai submit qua luong community species de duoc cap SpecCode >= 500000")
    print()
    print(f"  Da ghi {TODO}, can nguoi xem lai:")
    print(f"    {len(unresolved)} ten ban chua tra duoc ten khoa hoc")
    print(f"    {len(curated)} ten khoa hoc trong CSV khong khop FishBase")
    print(f"    {len(not_in_fishdex)} loai tiem ban nhung ETL khong nap vao FishDex")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
