"""
build_delta.py — sinh delta vá các cột DB bị đổ sai nguồn.

Mọi thứ ở đây đều là **cột đã có sẵn trong DB**, chỉ bị ETL đổ sai hoặc bỏ trống.
Không tạo bảng mới, không thêm cột, không xoá dòng nào.

Mỗi bản vá cho ra hai file, tách thư mục:

    csv/<tên>.csv   để mắt người soi trước khi chạy
    sql/<tên>.sql   các câu UPDATE rời rạc để nạp vào DB

Đích mặc định: D:\\Workspace\\FishLover\\Data\\exports_01082026\\FixIsssueETL_NULL_DATA
Đổi bằng `--out <thư_mục>`.

Nguyên tắc thiết kế
───────────────────
1.  KHÔNG lọc theo DB đích. Local có 4.192 loài, PROD có thể ít hơn. Sinh một file
    duy nhất cho toàn bộ phạm vi parquet; mã nào chưa có ở DB đích thì câu UPDATE
    khớp 0 dòng, tự bỏ qua. Cùng một file chạy được cả hai bên, và chạy lại sau khi
    PROD nạp thêm loài thì tự phủ nốt phần mới.

2.  Chỉ điền chỗ trống, không ghi đè. Mỗi câu UPDATE tự mang điều kiện "ô đang
    trống" (NULL, chuỗi rỗng, hoặc 0 với cột số). Dữ liệu tốt sẵn có không bao giờ
    bị đụng tới.

3.  Mỗi ô một câu UPDATE riêng, không bọc transaction. Câu nào lỗi chỉ mình nó hỏng,
    không kéo theo câu khác. Đứt giữa chừng thì chạy lại cả file: phần đã vá thành
    no-op nhờ điều kiện ở nguyên tắc 2, phần còn thiếu chạy tiếp.

4.  SQL thuần — không `\\set`, không `\\copy`, không lệnh meta nào của psql. Dán vào
    DataGrip / DBeaver / client bất kỳ đều chạy. Giá trị nhúng thẳng vào câu lệnh nên
    không phải kèm file CSV hay lo đường dẫn.

5.  File quá lớn thì tự cắt thành `_1`, `_2`, `_3` (xem MAX_STMT_PER_FILE) — DataGrip
    không mở nổi file vài MB.

Chạy
────
    cd Pipeline/local/FishDex
    PYTHONIOENCODING=utf-8 PYTHONPATH=. ./.venv/Scripts/python.exe build_delta.py \\
        "D:/Workspace/FishLover/Data/exports_01082026/UpdateFish_15082026/spec_codes.txt"

Phạm vi mặc định là bộ lọc Aquarium (2.043 loài) — CHƯA gồm lô bổ sung 2.149 loài
đã nạp bằng run_scoped.py. Truyền thêm file mã của các lô đó vào (nhiều file cũng
được) để delta phủ đúng những gì đang nằm trong DB.

Nạp vào DB
──────────
Mở file trong DataGrip và chạy. Thứ tự, thông tin kết nối và số kỳ vọng nằm ở
RUN.md cùng thư mục đích.
"""
from __future__ import annotations

import sys
from dataclasses import dataclass, field
from pathlib import Path
from typing import Callable

import polars as pl

from etl.config import PARQUET_DIR, PARQUET_FILES
from etl.filter import compute_spec_codes
# Dùng chung hàm chuyển đổi với loader — hai bên phải cho ra cùng một kết quả,
# nếu không thì loài cũ (vá bằng SQL) và loài mới (nạp bằng ETL) sẽ lệch nhau.
from etl.loaders.stocks import resilience_code
from etl.sources import age_by_spec, first_positive, troph_by_spec, weight_by_spec

# Đích mặc định nằm NGOÀI repo, cạnh README mô tả sự cố và cạnh lô UpdateFish_15082026.
# Delta là tài liệu vận hành dùng một lần cho mỗi lô, không phải mã nguồn — để trong
# repo thì lẫn với code và thêm ~7MB mỗi lần sinh. Đổi bằng --out <thư_mục>.
OUT_DIR = Path(r"D:\Workspace\FishLover\Data\exports_01082026\FixIsssueETL_NULL_DATA")

# Trần số câu mỗi file .sql. DataGrip không mở nổi file vài MB — dán vào console là
# treo, mà Run-from-file cũng ì. Vượt trần thì cắt thành <tên>_1.sql, _2, _3 ... với
# số câu chia đều. Cắt ở đâu cũng an toàn: các câu UPDATE độc lập hoàn toàn, không
# transaction, không thứ tự bắt buộc.
MAX_STMT_PER_FILE = 10_000


# ── Sinh SQL ─────────────────────────────────────────────────────────────────

def _literal(v) -> str:
    """Giá trị Python → literal SQL. Chuỗi thì nhân đôi dấu nháy đơn."""
    if isinstance(v, str):
        return "'" + v.replace("'", "''") + "'"
    if isinstance(v, bool):
        return "true" if v else "false"
    if isinstance(v, float):
        return repr(v)  # repr cho biểu diễn ngắn nhất mà round-trip đúng bit
    return str(v)


def _empty_cond(col: str, dtype: pl.DataType, zero_is_value: set[str]) -> str:
    """
    Điều kiện "ô này đang trống".

    Với cột số, 0 thường là dấu vết của bug `or 0` chứ không phải số liệu thật, nên
    mặc định coi 0 là trống. Ngoại lệ phải khai vào `zero_is_value` — ví dụ
    StockEnvironment.Resilience, ở đó 0 nghĩa là VeryLow, một giá trị hợp lệ.
    """
    if dtype == pl.String:
        return f'("{col}" IS NULL OR "{col}" = \'\')'
    if col in zero_is_value:
        return f'"{col}" IS NULL'
    return f'("{col}" IS NULL OR "{col}" = 0)'


@dataclass
class Fix:
    name: str
    table: str
    keys: list[str]               # cột định danh dòng ở DB
    values: list[str]             # cột cần vá
    build: Callable[[set[int]], pl.DataFrame]
    note: str = ""
    zero_is_value: set[str] = field(default_factory=set)


def _emit(fix: Fix, df: pl.DataFrame) -> None:
    # Tách hai thư mục: csv/ để soi và đối chiếu, sql/ để nạp. Đứng trong sql/ mà
    # chạy vòng lặp thì không vướng file csv cùng tên.
    (OUT_DIR / "csv").mkdir(parents=True, exist_ok=True)
    (OUT_DIR / "sql").mkdir(parents=True, exist_ok=True)
    csv_path = OUT_DIR / "csv" / f"{fix.name}.csv"


    df.write_csv(csv_path)

    schema = df.schema
    stmts: list[str] = []
    for row in df.iter_rows(named=True):
        where_key = " AND ".join(
            f'"{k}" = {_literal(row[k])}' for k in fix.keys
        )
        for col in fix.values:
            val = row[col]
            if val is None:
                continue
            stmts.append(
                f'UPDATE "{fix.table}" SET "{col}" = {_literal(val)}'
                f" WHERE {where_key} AND {_empty_cond(col, schema[col], fix.zero_is_value)};"
            )

    verify = [
        f'--   SELECT count(*) FILTER (WHERE {_empty_cond(col, schema[col], fix.zero_is_value)}) AS con_trong,'
        f' count(*) AS tong FROM "{fix.table}";'
        for col in fix.values
    ]

    # Chia đều thay vì đổ đầy 10.000 rồi để phần cuối lèo tèo — ba file 7.977 câu
    # dễ ước lượng tiến độ hơn 10.000 + 10.000 + 3.931.
    parts = max(1, -(-len(stmts) // MAX_STMT_PER_FILE))
    size = -(-len(stmts) // parts)
    written = []

    for i in range(parts):
        chunk = stmts[i * size:(i + 1) * size]
        suffix = f"_{i + 1}" if parts > 1 else ""
        path = OUT_DIR / "sql" / f"{fix.name}{suffix}.sql"

        # SQL thuần, không có lệnh meta của psql (\set, \copy) — dán thẳng vào
        # DataGrip / DBeaver / client nào cũng chạy.
        head = [f"-- {fix.name} — vá {fix.table}.{'/'.join(fix.values)}"]
        if parts > 1:
            head.append(f"-- PHẦN {i + 1}/{parts} — {len(chunk):,} câu."
                        f" Các phần độc lập, chạy thứ tự nào cũng được, nhưng phải chạy đủ cả {parts}.")
        if fix.note:
            head.append(f"-- {fix.note}")
        head += [
            "--",
            "-- Chỉ điền vào ô đang trống, không ghi đè. Chạy lại nhiều lần vô hại.",
            "-- Không bọc transaction: mỗi câu tự commit, câu lỗi không kéo theo câu khác.",
            "-- DataGrip: đặt Error handling = Ignore để một câu hỏng không dừng cả file.",
            "",
        ]

        tail = ["", "-- Kiểm tra (số đúng chỉ có sau khi đã chạy hết mọi phần):", *verify] \
            if i == parts - 1 or parts == 1 else \
            ["", f"-- Còn {parts - i - 1} phần nữa: {fix.name}_{i + 2}.sql ..."]

        path.write_text("\n".join(head + chunk + tail) + "\n", encoding="utf-8")
        written.append(path.name)

    print(f"  {fix.name:<22} {len(df):>7,} dòng → {len(stmts):>7,} câu UPDATE"
          f"   ({csv_path.name}, {' + '.join(written)})")


# ── Các bản vá ───────────────────────────────────────────────────────────────

def _genus_name(_: set[int]) -> pl.DataFrame:
    df = pl.read_parquet(PARQUET_DIR / PARQUET_FILES["genera"], columns=["GenCode", "GenName"])
    return (df.rename({"GenCode": "GenusCode", "GenName": "GenusName"})
              .filter(pl.col("GenusName").is_not_null() & (pl.col("GenusName") != ""))
              .unique(subset=["GenusCode"]).sort("GenusCode"))


def _stock_env(spec_codes: set[int]) -> pl.DataFrame:
    """
    pH/dH sai tên cột (viết thường chữ đầu), còn Resilience/BoundingRef/BoundingMethod
    thì loader **quên hẳn không đưa vào câu INSERT** — kiểu lỗi khác, cùng hậu quả.
    Resilience trong parquet là chữ, DB lưu số theo enum ResilienceLevel.
    """
    df = pl.read_parquet(
        PARQUET_DIR / PARQUET_FILES["stocks"],
        columns=["StockCode", "SpecCode", "pHMin", "pHMax", "dHMin", "dHMax",
                 "Resilience", "BoundingRef", "BoundingMethod"],
    ).filter(pl.col("SpecCode").is_in(list(spec_codes)))

    vals = ["PHMin", "PHMax", "DHMin", "DHMax", "Resilience", "BoundingRef", "BoundingMethod"]
    return (df.rename({"pHMin": "PHMin", "pHMax": "PHMax", "dHMin": "DHMin", "dHMax": "DHMax"})
              .with_columns(
                  pl.col("Resilience")
                    .map_elements(resilience_code, return_dtype=pl.Int64)
                    .alias("Resilience"),
                  pl.col("BoundingRef").cast(pl.String),
                  pl.col("BoundingMethod").cast(pl.String),
              )
              .drop("SpecCode")
              .filter(pl.any_horizontal(pl.col(vals).is_not_null()))
              .unique(subset=["StockCode"]).sort("StockCode"))


def _species_bio(spec_codes: set[int]) -> pl.DataFrame:
    """Weight và LongevityWild — gộp nguồn phụ, xem etl/sources.py."""
    df = pl.read_parquet(
        PARQUET_DIR / PARQUET_FILES["species"],
        columns=["SpecCode", "Weight", "WeightFemale", "LongevityWild", "LongevityCaptive"],
    ).filter(pl.col("SpecCode").is_in(list(spec_codes)))

    weight_alt = weight_by_spec(spec_codes)
    age_alt = age_by_spec(spec_codes)

    rows = []
    for r in df.iter_rows(named=True):
        code = r["SpecCode"]
        rows.append({
            "SpecCode": code,
            "Weight": first_positive(r["Weight"], r["WeightFemale"], weight_alt.get(code)),
            "LongevityWild": first_positive(r["LongevityWild"], age_alt.get(code)),
            "LongevityCaptive": first_positive(r["LongevityCaptive"]),
        })
    out = pl.DataFrame(rows, schema={"SpecCode": pl.Int64, "Weight": pl.Float64,
                                     "LongevityWild": pl.Float64, "LongevityCaptive": pl.Float64})
    return out.filter(
        pl.any_horizontal(pl.col("Weight", "LongevityWild", "LongevityCaptive").is_not_null())
    ).sort("SpecCode")


def _diet_troph(spec_codes: set[int]) -> pl.DataFrame:
    """
    Khoá là EcologyId, không phải SpecCode — "FeedingAndDiets" khoá theo EcologyId
    (= autoctr của ecology.parquet). Đó cũng là trần phủ của bản vá này: chỉ với tới
    các loài có dòng Ecology, khoảng 1.724 trong 4.192.
    """
    df = pl.read_parquet(
        PARQUET_DIR / PARQUET_FILES["ecology"],
        columns=["autoctr", "SpecCode", "DietTroph", "FoodTroph"],
    ).filter(pl.col("SpecCode").is_in(list(spec_codes)))

    troph_alt = troph_by_spec(spec_codes)

    rows = []
    for r in df.iter_rows(named=True):
        val = first_positive(r["DietTroph"], r["FoodTroph"], troph_alt.get(r["SpecCode"]))
        if val is not None:
            rows.append({"EcologyId": r["autoctr"], "DietTroph": val})
    return (pl.DataFrame(rows, schema={"EcologyId": pl.Int64, "DietTroph": pl.Float64})
              .unique(subset=["EcologyId"]).sort("EcologyId"))


def _occurrence_locality(spec_codes: set[int]) -> pl.DataFrame:
    """
    "Occurrences" không có khoá tự nhiên — Id do DB sinh, loader thì DELETE rồi
    INSERT lại. Nhưng bộ ba (SpecCode, LatitudeDec, LongitudeDec) định danh đủ chặt:
    93% nhóm chỉ ứng với đúng một Locality. Số nhóm mơ hồ còn lại bị loại thẳng —
    thà bỏ sót còn hơn ghi nhầm địa danh, và cũng không phải xoá dòng nào.
    """
    df = pl.read_parquet(
        PARQUET_DIR / PARQUET_FILES["occurrence"],
        columns=["SpecCode", "LatitudeDec", "LongitudeDec", "Locality1"],
    ).filter(
        pl.col("SpecCode").is_in(list(spec_codes))
        & pl.col("LatitudeDec").is_not_null()
        & pl.col("LongitudeDec").is_not_null()
        & pl.col("Locality1").is_not_null()
        & (pl.col("Locality1") != "")
    ).rename({"Locality1": "Locality"})

    keys = ["SpecCode", "LatitudeDec", "LongitudeDec"]
    counts = df.group_by(keys).agg(pl.col("Locality").n_unique().alias("_n"))
    ambiguous = counts.filter(pl.col("_n") > 1)
    if len(ambiguous):
        print(f"  [occurrence] Bỏ qua {len(ambiguous):,} nhóm toạ độ ứng với nhiều địa danh.")

    return (df.join(counts.filter(pl.col("_n") == 1).drop("_n"), on=keys, how="inner")
              .unique(subset=keys).sort(keys))


FIXES = [
    Fix("genus_name", "Genuses", ["GenusCode"], ["GenusName"],
        _genus_name, "Loader cũ hỏi cột 'Genus'/'GenusName', parquet tên là 'GenName'."),
    Fix("stock_env", "StockEnvironment", ["StockCode"],
        ["PHMin", "PHMax", "DHMin", "DHMax", "Resilience", "BoundingRef", "BoundingMethod"],
        _stock_env,
        "pH/dH sai tên cột; Resilience/Bounding* bị quên khỏi câu INSERT của loader.",
        zero_is_value={"Resilience"}),  # 0 = VeryLow, là giá trị thật
    Fix("species_bio", "Species", ["SpecCode"], ["Weight", "LongevityWild", "LongevityCaptive"],
        _species_bio, "species.parquet để trống gần hết; bổ sung từ popchar/popgrowth/estimate."),
    Fix("diet_troph", "FeedingAndDiets", ["EcologyId"], ["DietTroph"],
        _diet_troph, "Bổ sung từ FoodTroph rồi estimate.Troph. Trần phủ ~1.724 loài."),
    Fix("occurrence_locality", "Occurrences", ["SpecCode", "LatitudeDec", "LongitudeDec"], ["Locality"],
        _occurrence_locality, "Loader cũ hỏi 'Locality', parquet tên là 'Locality1'."),
]


def _extra_codes(paths: list[str]) -> set[int]:
    """Đọc file mã của các lô nạp thêm — cùng định dạng run_scoped.py nhận."""
    codes: set[int] = set()
    for p in paths:
        raw = "\n".join(
            l for l in Path(p).read_text(encoding="utf-8").splitlines()
            if not l.strip().startswith("#")
        )
        codes.update(int(t) for t in raw.replace(",", " ").split() if t.strip().isdigit())
        print(f"  [extra] +{len(codes):,} mã tích luỹ từ {Path(p).name}")
    return codes


def main(argv: list[str] | None = None) -> None:
    global OUT_DIR
    argv = list(sys.argv[1:] if argv is None else argv)

    if "--out" in argv:
        i = argv.index("--out")
        OUT_DIR = Path(argv[i + 1])
        del argv[i:i + 2]

    print(f"[Step 0] Tính spec_codes ...")
    spec_codes = compute_spec_codes() | _extra_codes(argv)
    print(f"  {len(spec_codes):,} loài trong phạm vi.\n")

    print("[Step 1] Sinh delta ...")
    for fix in FIXES:
        _emit(fix, fix.build(spec_codes))

    print(f"\nXong. Xem thư mục {OUT_DIR}")


if __name__ == "__main__":
    main()
