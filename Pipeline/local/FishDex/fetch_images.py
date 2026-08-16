"""
Tải file ảnh thật từ FishBase về thư mục đúng hình dạng bucket R2.

    python fetch_images.py <systemimages_delta.csv> <thu_muc_ra> [so_luong]

Việc này KHÔNG có trong ETL Python — `etl/loaders/images.py` chỉ ghi metadata vào
bảng `SystemImages`, không tải file. Phần tải nằm ở project C# cũ
(`DataProcessor.CrawSystemImagesAsync`), nhưng quy ước đường dẫn của nó đã lạc hậu:
nó ghi `init-data/{SpecCode}/{Id}{ext}` trong khi app hiện đọc `{SpecCode}/{Id}{ext}`.

Bằng chứng cho đường dẫn đúng — URL presigned mà API PROD trả về:
    https://system-image.<account>.r2.cloudflarestorage.com/4833/6dabbf4b-....jpg
Không có tiền tố. Khớp với `SystemImage.ObjectKey => $"{SpecCode}/{Id}{ext}"`.

⚠️ TÊN FILE LÀ GUID `Id`, KHÔNG PHẢI `PicName` của FishBase.
`Name` trong CSV (vd `Amura_u0.jpg`) chỉ dùng để dựng URL nguồn và lấy phần mở rộng.

Chạy lại được: file đã tải thì bỏ qua, nên đứt mạng giữa chừng cứ chạy lại.

Số luồng mặc định 6. ĐỪNG nâng quá cao — FishBase là nguồn miễn phí, dội hàng
nghìn request song song dễ bị chặn IP và hỏng cả lô. 6 luồng cho khoảng 4 ảnh/giây,
đủ nhanh mà vẫn nhẹ tay.
"""
from __future__ import annotations
import csv
import sys
import threading
import time
import urllib.error
import urllib.request
from concurrent.futures import ThreadPoolExecutor, as_completed
from pathlib import Path

NGUON = "https://www.fishbase.org.au/v4/images/species"
NGHI = 0.1           # giây mỗi luồng nghỉ giữa hai request
TIMEOUT = 30
LUONG_MAC_DINH = 6
UA = "Mozilla/5.0 (compatible; TheFishLover/1.0; +https://fishlover.org)"

_khoa = threading.Lock()
_dem = {"tai": 0, "bo_qua": 0}


def tai_mot(r: dict, out_dir: Path) -> tuple | None:
    """Trả về None nếu ổn, hoặc tuple lỗi để ghi ra file."""
    spec, gid, name = r["SpecCode"].strip(), r["Id"].strip(), r["Name"].strip()
    if not name:
        return (spec, gid, name, "Name rong")

    ext = Path(name.split("?")[0].split("#")[0]).suffix or ".jpg"
    dich = out_dir / spec / f"{gid}{ext}"

    if dich.exists() and dich.stat().st_size > 0:
        with _khoa:
            _dem["bo_qua"] += 1
        return None

    dich.parent.mkdir(parents=True, exist_ok=True)
    try:
        req = urllib.request.Request(f"{NGUON}/{name}", headers={"User-Agent": UA})
        with urllib.request.urlopen(req, timeout=TIMEOUT) as resp:
            data = resp.read()
        if not data:
            return (spec, gid, name, "file rong")
        dich.write_bytes(data)
        with _khoa:
            _dem["tai"] += 1
    except urllib.error.HTTPError as e:
        return (spec, gid, name, f"HTTP {e.code}")
    except Exception as e:                       # timeout, DNS, reset...
        return (spec, gid, name, type(e).__name__)
    finally:
        time.sleep(NGHI)
    return None


def main() -> int:
    if len(sys.argv) < 3:
        print(__doc__)
        return 1

    csv_path, out_dir = Path(sys.argv[1]), Path(sys.argv[2])
    luong = int(sys.argv[3]) if len(sys.argv) > 3 else LUONG_MAC_DINH
    if not csv_path.exists():
        print(f"Khong tim thay {csv_path}", file=sys.stderr)
        return 1

    csv.field_size_limit(10_000_000)
    with csv_path.open(encoding="utf-8", newline="") as f:
        rows = list(csv.DictReader(f))

    out_dir.mkdir(parents=True, exist_ok=True)
    loi_path = out_dir.parent / "images_failed.csv"

    print(f"  {len(rows):,} anh · {luong} luong · nguon {NGUON}")
    t0 = time.time()
    loi = []

    with ThreadPoolExecutor(max_workers=luong) as ex:
        futs = {ex.submit(tai_mot, r, out_dir): r for r in rows}
        for i, fut in enumerate(as_completed(futs), start=1):
            kq = fut.result()
            if kq:
                loi.append(kq)
            if i % 200 == 0:
                dt = time.time() - t0
                toc = i / dt if dt else 0
                con = (len(rows) - i) / toc if toc else 0
                print(f"  {i:,}/{len(rows):,}  tai {_dem['tai']:,} · bo qua "
                      f"{_dem['bo_qua']:,} · loi {len(loi):,}  "
                      f"({toc:.1f}/s, con ~{con/60:.0f} phut)", flush=True)

    if loi:
        moi = not loi_path.exists()
        with loi_path.open("a", newline="", encoding="utf-8") as f:
            w = csv.writer(f)
            if moi:
                w.writerow(["SpecCode", "Id", "Name", "LyDo"])
            w.writerows(loi)

    print()
    print(f"  Da tai   : {_dem['tai']:,}")
    print(f"  Bo qua   : {_dem['bo_qua']:,} (da co san)")
    print(f"  Loi      : {len(loi):,}" + (f" -> {loi_path.name}" if loi else ""))
    print(f"  Thoi gian: {time.time()-t0:.0f}s")
    print()
    print("  Upload NGUYEN cau truc thu muc nay vao GOC bucket 'system-image'.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
