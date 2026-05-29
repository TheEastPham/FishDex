"""
ETL configuration — chỉnh sửa tại đây trước khi chạy.
"""
import os
from pathlib import Path

# ── Database ──────────────────────────────────────────────────────────────────
# Override bằng env var: DB_URL=postgresql://user:pass@host:port/db
DB_URL = os.getenv(
    "DB_URL",
    "postgresql://fishdex:fishdex_local_pwd@localhost:5433/fishdex",
)

# ── Parquet file paths ────────────────────────────────────────────────────────
PARQUET_DIR = Path(__file__).parent.parent / "parquetData"

# Map: loader name → parquet filename (đổi nếu file tên khác)
PARQUET_FILES = {
    "families":    "families.parquet",
    "genera":      "genera.parquet",
    "species":     "species.parquet",
    "stocks":      "stocks.parquet",
    "ecology":     "ecology.parquet",
    "morphdat":    "morphdat.parquet",
    "ecosystemref":"ecosystemref.parquet",
    "ecosystem":   "ecosystem.parquet",
    "occurrence":  "occurrence.parquet",
    "comnames":    "comnames.parquet",
    "images":      "picturesmain.parquet",
}

# ── Aquarium filter ──────────────────────────────────────────────────────────
# Chạy inspect.py trước để xem giá trị thực tế trong parquet.
# Các giá trị FishBase thường dùng: "highly commercial", "commercial",
#   "of minor importance", "never/rarely", "potential"
AQUARIUM_VALUES: set[str] = {
    "highly commercial",
    "commercial",
    # "of minor importance",  # bỏ comment nếu muốn lấy thêm
}

# Lấy thêm brackish water không? (Fresh=0, Brack=1)
INCLUDE_BRACKISH = False

# ── Batch size ────────────────────────────────────────────────────────────────
BATCH_SIZE = 500
