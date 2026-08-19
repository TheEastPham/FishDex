"""
Sinh 2 file seed: dòng lai vào SpeciesSnapshots, và market Việt Nam vào TradedSpecies.

    python seed/build_seed.py <thu_muc_ra>

Đọc `cultivars.csv`, `cultivar-codes.csv`, `market-vn.csv` và DB local.
CHỈ ĐỌC — không ghi gì vào DB.

── Dòng lai kế thừa gì từ loài cha ───────────────────────────────────────────
Kế thừa **hoá học nước** (`WaterType`, Temp, pH, dH) vì dòng lai cùng loài sinh học
với cha nên nhu cầu nước giống nhau — Ranchu cần đúng nước của cá vàng.

KHÔNG kế thừa `Length`, `CareLevel`, `MinTankLiters`: Ranchu ngắn và tròn hơn cá
vàng thường, lại khó nuôi hơn hẳn. Chép sang là nói sai với người đọc.

⚠️ Lưu ý khi đọc số: khoảng nhiệt của FishBase là khoảng **sinh tồn ngoài tự nhiên**,
không phải khoảng nuôi khuyến nghị. `Carassius auratus` ghi 0–41°C — đúng về sinh
học, nhưng đừng hiểu là nuôi Ranchu ở 0°C được.

── Vì sao TradeStatus để TRỐNG ───────────────────────────────────────────────
`market-vn.csv` chỉ chứng minh loài đó CÓ BÁN, không nói mức phổ biến. Suy ra
"Common" từ việc một tiệm có bán là bịa. Bộ lọc mức phổ biến cũng đã bị bỏ khỏi
UI v1 (task D1) nên cột này chưa ai dùng tới.
"""
from __future__ import annotations
import csv
import subprocess
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
NGAY = "2026-08-15"
CC_VN = "704"

# Dòng lai: TẮT. Bật lại thì `snapshots_seed.csv` cũng phải nạp cùng lúc — mã ≥ 500000
# không có row trong `Species`, tên chỉ đến từ `SpeciesSnapshots`.
NAP_DONG_LAI = False

# Nhãn tiệm ghi tên ĐỒNG NGHĨA CŨ, FishDex lưu tên hiện hành. Không có bảng này thì
# `JOIN ON SpeciesName` trượt và 5 loài bị loại KHÔNG BÁO — đã xảy ra một lần.
# Giữ nguyên chữ trong CSV để còn đối chiếu được với nhãn tiệm.
DONG_NGHIA = {
    "Brochis splendens":       "Corydoras splendens",
    "Brochis multiradiatus":   "Corydoras multiradiatus",
    "Corydoras barbatus":      "Scleromystax barbatus",
    "Crossocheilus siamensis": "Crossocheilus oblongus",
    "Hemigrammus bleheri":     "Petitella bleheri",
}

# Enum, đã đối chiếu BackEndProject/.../Entity/Market/TradedSpecies.cs
ORIGIN_ADMIN_SEED = 0
STATUS_APPROVED = 1
LEGAL_LEGAL = 0
# SpeciesSnapshot enums
DATASOURCE_COMMUNITY = 1
POPULATED_MANUAL = 1
KIND_HYBRID = 1
WATERTYPE_FRESH = 1


def psql(sql: str) -> list[str]:
    r = subprocess.run(
        ["docker", "exec", "fishdex_postgres", "psql", "-U", "fishdex", "-d", "fishdex",
         "-t", "-A", "-c", sql],
        capture_output=True, text=True, encoding="utf-8")
    if r.returncode:
        print(r.stderr, file=sys.stderr)
        raise SystemExit(1)
    return [l for l in r.stdout.splitlines() if l.strip()]


def doc(path: Path, **kw) -> list[dict]:
    with path.open(encoding="utf-8-sig", newline="") as f:
        return list(csv.DictReader(f))


def main() -> int:
    if len(sys.argv) < 2:
        print(__doc__)
        return 1
    out = Path(sys.argv[1])
    out.mkdir(parents=True, exist_ok=True)

    cultivars = doc(HERE / "cultivars.csv")
    # Vòng 1 (4 tiệm) + vòng 2 (cacanhthaihoa, auraaquatic). Để 2 file riêng thay vì
    # gộp: giữ được xuất xứ từng vòng, và không phải sửa file vòng 1 đã curate xong.
    market = doc(HERE / "market-vn.csv")
    r2 = HERE / "market-vn-r2.csv"
    if r2.exists():
        market += doc(r2)

    # Bảng mã — khoá theo `khoa`, bỏ dòng chú thích
    with (HERE / "cultivar-codes.csv").open(encoding="utf-8-sig") as f:
        ma = {r["khoa"]: r["specCode"].strip()
              for r in csv.DictReader(l for l in f if not l.startswith("#"))}

    thieu = [c["khoa"] for c in cultivars if not ma.get(c["khoa"])]
    if thieu:
        print(f"{len(thieu)} dong lai CHUA CO MA: {thieu[:5]}", file=sys.stderr)
        return 1

    # ── Dữ liệu loài cha ────────────────────────────────────────────────────
    cha_codes = sorted({c["loaiChaSpecCode"].strip()
                        for c in cultivars if c["loaiChaSpecCode"].strip()})
    rows = psql(f'''
        SELECT DISTINCT ON (s."SpecCode")
               s."SpecCode"||'|'||COALESCE(f."Name",'')||'|'||COALESCE(g."GenusName",'')
               ||'|'||s."WaterType"||'|'||COALESCE(e."TempMin"::text,'')||'|'||COALESCE(e."TempMax"::text,'')
               ||'|'||COALESCE(e."PHMin"::text,'')||'|'||COALESCE(e."PHMax"::text,'')
               ||'|'||COALESCE(e."DHMin"::text,'')||'|'||COALESCE(e."DHMax"::text,'')
        FROM "Species" s
        LEFT JOIN "Genuses" g ON g."GenusCode"=s."GenusCode"
        LEFT JOIN "Families" f ON f."Id"=g."FamId"
        LEFT JOIN "Stock" st ON st."SpecCode"=s."SpecCode"
        LEFT JOIN "StockEnvironment" e ON e."StockCode"=st."StockCode"
        WHERE s."SpecCode" IN ({",".join(cha_codes)})
        ORDER BY s."SpecCode", e."TempMin" NULLS LAST;''')
    cha = {}
    for l in rows:
        p = l.split("|")
        cha[p[0]] = dict(ho=p[1], chi=p[2], nuoc=p[3],
                         tmin=p[4], tmax=p[5], phmin=p[6], phmax=p[7],
                         dhmin=p[8], dhmax=p[9])

    # ── File 1: SpeciesSnapshots ────────────────────────────────────────────
    F1 = ["SpecCode", "DataSource", "IsVerified", "SpeciesName", "FamilyName",
          "GenusName", "CommonName", "WaterType", "TempMin", "TempMax",
          "PhMin", "PhMax", "DhMin", "DhMax", "Kind", "PopulatedFrom", "PopulatedAt"]
    snap = []
    for c in cultivars:
        p = cha.get(c["loaiChaSpecCode"].strip(), {})
        ten = c["tenQuocTe"].strip()
        cha_ten = c["loaiChaTen"].strip()
        snap.append({
            "SpecCode": ma[c["khoa"]],
            "DataSource": DATASOURCE_COMMUNITY,
            "IsVerified": "true",
            # Quy uoc cultivar quoc te: Chi loai 'Ten dong'
            "SpeciesName": f"{cha_ten} '{ten}'" if cha_ten else ten,
            "FamilyName": p.get("ho", ""),
            "GenusName": p.get("chi", ""),
            "CommonName": ten,
            "WaterType": p.get("nuoc") or WATERTYPE_FRESH,
            "TempMin": p.get("tmin", ""), "TempMax": p.get("tmax", ""),
            "PhMin": p.get("phmin", ""), "PhMax": p.get("phmax", ""),
            "DhMin": p.get("dhmin", ""), "DhMax": p.get("dhmax", ""),
            "Kind": KIND_HYBRID,
            "PopulatedFrom": POPULATED_MANUAL,
            "PopulatedAt": NGAY,
        })
    with (out / "snapshots_seed.csv").open("w", newline="", encoding="utf-8") as f:
        w = csv.DictWriter(f, fieldnames=F1); w.writeheader(); w.writerows(snap)

    # ── File 2: TradedSpecies ───────────────────────────────────────────────
    loai = [r for r in market if r["loai"] == "loai" and r["tenKhoaHoc"].strip()]
    for r in loai:                          # quy về tên hiện hành trước khi tra DB
        n = r["tenKhoaHoc"].strip()
        r["tenHienHanh"] = DONG_NGHIA.get(n, n)
    ten2code = {}
    names = sorted({r["tenHienHanh"] for r in loai})
    vals = ",".join("('" + n.replace("'", "''") + "')" for n in names)
    for l in psql(f'''WITH x(n) AS (VALUES {vals})
        SELECT x.n||'|'||s."SpecCode" FROM x JOIN "Species" s ON s."SpeciesName"=x.n;'''):
        n, sc = l.split("|"); ten2code[n] = sc

    F2 = ["CountryCode", "SpecCode", "TradeStatus", "LegalStatus",
          "Origin", "Status", "LastConfirmedAt"]
    traded, khong_khop = [], []
    seen = set()
    for r in loai:
        sc = ten2code.get(r["tenHienHanh"])
        if not sc:
            khong_khop.append(r["tenHienHanh"]); continue
        if sc in seen:                      # 2 tên bán cùng một loài, vd chuột Venezuela/Albino
            continue
        seen.add(sc)
        traded.append(dict(CountryCode=CC_VN, SpecCode=sc, TradeStatus="",
                           LegalStatus=LEGAL_LEGAL, Origin=ORIGIN_ADMIN_SEED,
                           Status=STATUS_APPROVED, LastConfirmedAt=NGAY))
    # Dòng lai bán ở VN — nối qua bảng tra TƯỜNG MINH, không suy từ cột `nguon`.
    # `cultivars.csv` ghi nguồn TÀI LIỆU (wikipedia-koi...), còn bằng chứng thị
    # trường nằm ở `market-vn.csv`. Hai trục khác nhau nên phải có bảng nối riêng.
    #
    # ⛔ TẠM DỪNG (19/08/2026). Toàn bộ dòng lai đang dừng — độ phủ registry thấp và
    # không có ảnh dùng được, xem README của thư mục seed. Nhánh này TRƯỚC ĐÂY chạy vô
    # điều kiện nên vẫn nhét 3 mã ≥ 500000 vào `traded_species_seed.csv` dù dòng lai đã
    # được chốt dừng; mà `SpeciesSnapshots` không nạp thì thẻ hiện chữ `SpecCode 500025`
    # (MarketService.cs:233). Bật lại cùng lúc với `snapshots_seed.csv`, không bật lẻ.
    lai_vn = 0
    for m in (doc(HERE / "vn-cultivar-map.csv") if NAP_DONG_LAI else []):
        khoa = m["khoa"].strip()
        if not khoa:                        # cố ý để trống — không có trong registry
            continue
        sc = ma.get(khoa)
        if not sc:
            print(f"  CANH BAO: khoa '{khoa}' trong vn-cultivar-map.csv "
                  f"khong co trong cultivar-codes.csv", file=sys.stderr)
            continue
        if sc in seen:                      # Ping Pong và trân châu cùng trỏ pearlscale
            continue
        seen.add(sc); lai_vn += 1
        traded.append(dict(CountryCode=CC_VN, SpecCode=sc, TradeStatus="",
                           LegalStatus=LEGAL_LEGAL, Origin=ORIGIN_ADMIN_SEED,
                           Status=STATUS_APPROVED, LastConfirmedAt=NGAY))
    with (out / "traded_species_seed.csv").open("w", newline="", encoding="utf-8") as f:
        w = csv.DictWriter(f, fieldnames=F2); w.writeheader(); w.writerows(traded)

    # ── File 3: CommonNames tiếng Việt ──────────────────────────────────────
    # `tenBan` là nhãn tiệm cá cảnh VN in trên sản phẩm — đó CHÍNH LÀ tên bản ngữ.
    # Trước đây chỉ dùng nó để tra SpecCode rồi bỏ, nên trang market hiện
    # "Chưa có tên tiếng Việt" cho 120/123 loài dù dữ liệu đã nằm trong tay.
    # FishBase chỉ có 4 tên tiếng Việt trong nhóm này và đều sai dấu ("Cá bay màu"),
    # nhưng tất cả đều IsPreferred=false nên tên seed (IsPreferred=true, Rank=1) thắng.
    F3 = ["SpecCode", "ComName", "Language", "CountryCode", "NameType",
          "IsPreferred", "Rank", "IsVerified", "Remarks"]
    ten_viet, hang = [], {}
    for r in loai:
        sc, ten = ten2code.get(r["tenHienHanh"]), r["tenBan"].strip()
        if not sc or not ten:
            continue
        if any(t["SpecCode"] == sc and t["ComName"] == ten for t in ten_viet):
            continue
        # Một loài có thể mang 2 nhãn tiệm (chuột Venezuela / Albino) — cả hai đều là
        # tên bản ngữ thật, giữ cả, nhãn đầu làm preferred.
        hang[sc] = hang.get(sc, 0) + 1
        ten_viet.append(dict(SpecCode=sc, ComName=ten, Language="Vietnamese",
                             CountryCode=CC_VN, NameType="Vernacular",
                             IsPreferred=str(hang[sc] == 1).lower(), Rank=hang[sc],
                             IsVerified="true",
                             Remarks=f"seed nhan tiem ca canh VN {NGAY}; nguon {r['nguon']}"))
    ten_viet.sort(key=lambda x: (int(x["SpecCode"]), x["Rank"]))
    with (out / "common_names_seed_vn.csv").open("w", newline="", encoding="utf-8") as f:
        w = csv.DictWriter(f, fieldnames=F3); w.writeheader(); w.writerows(ten_viet)

    print(f"  snapshots_seed.csv      : {len(snap):>4} dong lai")
    print(f"  common_names_seed_vn.csv: {len(ten_viet):>4} ten tieng Viet"
          f"  ({len({x['SpecCode'] for x in ten_viet})} loai)")
    print(f"  traded_species_seed.csv : {len(traded):>4} loai ban o VN"
          f"  ({len(traded)-lai_vn} FishBase + {lai_vn} dong lai)")
    if khong_khop:
        print(f"  KHONG khop SpecCode ({len(khong_khop)}): {', '.join(khong_khop)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
