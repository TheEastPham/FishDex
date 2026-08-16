"""
audit.py — hậu kiểm độ phủ sau mỗi lần nạp.

VÌ SAO CẦN
──────────
Loader chỉ đếm **số dòng gửi đi**, không biết dòng đó có nghĩa hay không. Nên một
cột hỏng toàn bộ vẫn báo `Inserted/Updated: 11.040` như thường. Ba lỗi tìm được
ngày 15/08/2026 (`Genuses.GenusName`, `StockEnvironment` pH/dH, `Occurrences.Locality`)
đều thuộc kiểu đó: sai tên cột parquet → `.get()` trả None → cột rỗng 100% → im lặng.

Việc này nghiêm trọng hơn bình thường vì **ETL không bao giờ chạy lại cho loài đã
có trong FishDex** (`images.py` sinh GUID mới, mà GUID là tên file trên R2 — xem
run_scoped.py). Tức mỗi lô loài mới chỉ có **đúng một lần** để nạp cho đúng. Nạp
hỏng là phải đi đường vá thủ công bằng build_delta.py.

CÁCH LÀM
────────
Không dựa vào danh sách cột đoán trước — quét **mọi cột** của các bảng ETL ghi, tìm
cột rỗng 100%. Cách này bắt được cả những lỗi chưa ai nghĩ tới, khác với việc liệt
kê tay các cột "đáng lẽ phải có dữ liệu".

Cột đã xác minh là FishBase không có thật thì khai vào KNOWN_EMPTY để chúng không
lấn át tín hiệu. Cột rỗng nào chưa nằm trong đó = đáng nghi, phải soi.

Bỏ qua cột boolean: FishBase đánh dấu rất thưa (Schooling chỉ 142/12.566 dòng),
gần như cột nào cũng "rỗng" theo nghĩa toàn false, báo lên chỉ nhiễu.
"""
from __future__ import annotations

# Cột rỗng 100% ĐÃ ĐỐI CHIẾU PARQUET và xác nhận nguồn cũng trống — không phải bug.
# Chỉ thêm vào đây sau khi thực sự mở parquet ra kiểm, đừng thêm để cho đỡ đỏ màn hình.
KNOWN_EMPTY: set[tuple[str, str]] = {
    ("StockEnvironment", "EnvTemp"),           # kiểm 15/08: parquet có cột, rỗng sạch
    ("StockEnvironment", "TempPref25"),
    ("StockEnvironment", "TempPref50"),
    ("StockEnvironment", "TempPref75"),
    ("Species", "AuthorRef"),                  # kiểm 16/08: 0/4.192 trong parquet
    ("StockConservation", "CMS"),              # kiểm 16/08: 0/4.277
    ("Ecologies", "TS"),                       # kiểm 16/08: 0/1.737
    ("Associations", "SchoolingFrequency"),    # kiểm 16/08: 0/1.737
    ("Associations", "SchoolingLifestage"),
    ("Associations", "ShoalingFrequency"),
    ("Associations", "ShoalingLifestage"),
    ("CircadianBehaviors", "Circadian1"),      # kiểm 16/08: cả nhóm Circadian* rỗng
    ("CircadianBehaviors", "Circadian2"),
    ("CircadianBehaviors", "Circadian3"),
    ("CircadianBehaviors", "BioAspect1"),
    ("CircadianBehaviors", "BioAspect2"),
    ("CircadianBehaviors", "BioAspect3"),
    ("CircadianBehaviors", "RemarksCircadian"),
    ("CircadianBehaviors", "CircadianRef"),
    ("CircadianBehaviors", "CircadianAlsoRef"),
}

# Cột nằm trong bảng ETL nhưng do ứng dụng ghi, không phải ETL — rỗng là bình thường.
NOT_FROM_ETL: set[tuple[str, str]] = {
    ("CommonNames", "ReviewedBy"),
    ("CommonNames", "RejectionReason"),
}

# Bảng không do ETL nạp — dữ liệu cộng đồng, cache, bảng hệ thống.
SKIP_PREFIX = ("__EF", "AspNet", "OpenIddict", "_delta_")
SKIP_TABLES = {
    "SpeciesSnapshots", "SpeciesChunks", "SpeciesMedia", "TradedSpecies",
    "Cultivars", "Contests", "ContestEntries",
}


def _fill_expr(col: str, dtype: str) -> str | None:
    """Biểu thức đếm ô CÓ dữ liệu. Trả None nếu cột không nên kiểm."""
    if dtype == "boolean":
        return None  # thưa hợp lệ, xem docstring
    if dtype in ("text", "character varying", "character"):
        return f'count(*) FILTER (WHERE "{col}" IS NOT NULL AND "{col}" <> \'\')'
    if dtype in ("double precision", "real", "numeric", "integer", "bigint", "smallint"):
        return f'count(*) FILTER (WHERE "{col}" IS NOT NULL AND "{col}" <> 0)'
    return f'count("{col}")'


def check(conn, verbose: bool = False) -> list[tuple[str, str, int]]:
    """
    Quét các bảng ETL, trả về [(bảng, cột, tổng_dòng)] của những cột rỗng 100%
    chưa khai trong KNOWN_EMPTY. In báo cáo ra console.
    """
    with conn.cursor() as cur:
        cur.execute("""
            SELECT table_name, column_name, data_type
            FROM information_schema.columns
            WHERE table_schema = 'public'
            ORDER BY table_name, ordinal_position
        """)
        rows = cur.fetchall()

    by_table: dict[str, list[tuple[str, str]]] = {}
    for table, col, dtype in rows:
        if table.startswith(SKIP_PREFIX) or table in SKIP_TABLES:
            continue
        by_table.setdefault(table, []).append((col, dtype))

    suspects: list[tuple[str, str, int]] = []
    known_hit = 0

    with conn.cursor() as cur:
        for table, cols in by_table.items():
            checked = [(c, e) for c, d in cols if (e := _fill_expr(c, d)) is not None]
            if not checked:
                continue
            cur.execute(
                f'SELECT count(*), {", ".join(e for _, e in checked)} FROM "{table}"'
            )
            result = cur.fetchone()
            total, counts = result[0], result[1:]
            if total == 0:
                suspects.append((table, "*", 0))  # bảng rỗng hoàn toàn
                continue
            for (col, _), filled in zip(checked, counts):
                if filled:
                    continue
                if (table, col) in KNOWN_EMPTY or (table, col) in NOT_FROM_ETL:
                    known_hit += 1
                else:
                    suspects.append((table, col, total))

    print(f"\n[audit] Quét {len(by_table)} bảng — {len(suspects)} cột rỗng 100% cần soi"
          f" ({known_hit} cột đã khai KNOWN_EMPTY, bỏ qua).")
    for table, col, total in suspects:
        if col == "*":
            print(f"  ⚠  {table:<28} BẢNG RỖNG — loader báo thành công nhưng không có dòng nào")
        else:
            print(f"  ⚠  {table:<28} {col:<24} 0 / {total:,}")
    if suspects:
        print("  → Đối chiếu tên cột code đòi với tên cột thật trong parquet trước khi kết luận.")
        print("  → Nếu parquet cũng trống thật thì khai vào KNOWN_EMPTY trong etl/audit.py.")
    return suspects
