"""
DB connection và helper utilities.
"""
from __future__ import annotations
import math
import psycopg2
from .config import DB_URL, BATCH_SIZE


def connect():
    """Trả về psycopg2 connection tới PostgreSQL."""
    return psycopg2.connect(DB_URL)


def to_bool(val) -> bool:
    if val is None:
        return False
    if isinstance(val, bool):
        return val
    try:
        return int(val or 0) != 0
    except (ValueError, TypeError):
        return False


def to_float(val) -> float | None:
    if val is None:
        return None
    try:
        f = float(val)
        return None if math.isnan(f) else f
    except (TypeError, ValueError):
        return None


def to_int(val) -> int | None:
    if val is None:
        return None
    try:
        return int(val)
    except (TypeError, ValueError):
        return None


def to_str(val) -> str | None:
    if val is None:
        return None
    s = str(val).strip()
    return s if s else None


def execute_upsert(conn, sql: str, rows: list, table: str) -> tuple[int, int]:
    """
    Chạy executemany với UPSERT SQL.
    Trả về (inserted, skipped).
    Mỗi BATCH_SIZE rows là 1 lần executemany; nếu batch fail thì fallback row-by-row.
    """
    if not rows:
        print(f"  [{table}] Không có dữ liệu để load.")
        return 0, 0

    inserted = skipped = 0
    with conn.cursor() as cur:
        for i in range(0, len(rows), BATCH_SIZE):
            batch = rows[i : i + BATCH_SIZE]
            try:
                cur.executemany(sql, batch)
                inserted += len(batch)
            except Exception:
                conn.rollback()
                # Fallback: row-by-row
                for row in batch:
                    try:
                        cur.execute(sql, row)
                        inserted += 1
                    except Exception as e:
                        if skipped == 0:
                            print(f"  [{table}] First skip error: {e}")
                        skipped += 1
                        conn.rollback()
    conn.commit()
    print(f"  [{table}] Inserted/Updated: {inserted:,} | Skipped: {skipped:,}")
    return inserted, skipped


def delete_by_spec_codes(conn, table: str, pk_col: str, spec_codes: set[int]):
    """DELETE rows FROM table WHERE spec_col IN spec_codes."""
    codes = list(spec_codes)
    with conn.cursor() as cur:
        cur.execute(
            f'DELETE FROM "{table}" WHERE "{pk_col}" = ANY(%s)',
            (codes,),
        )
    conn.commit()
    print(f"  [{table}] Cleared existing rows for {len(codes):,} spec_codes.")
