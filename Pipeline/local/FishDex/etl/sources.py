"""
Nguồn phụ cho các cột mà parquet chính để trống gần hết.

Đo trên 4.192 loài của bộ hiện tại (15/08/2026):

    Weight          species.Weight 243  ∪ popchar.Wmax 224            → 327
    LongevityWild   species.Longevity* 97 ∪ popchar.tmax 69
                    ∪ popgrowth.tmax 42 ∪ estimate.AgeMax 149         → 211
    DietTroph       ecology.DietTroph 126 ∪ FoodTroph 1.162
                    ∪ estimate.Troph 4.192                            → trần 1.724 (*)

(*) estimate.Troph phủ đủ 4.192 loài nhưng DietTroph nằm ở "FeedingAndDiets",
    bảng này khoá theo EcologyId nên chỉ với tới 1.724 loài có dòng Ecology.
    Muốn phủ hết phải thêm cột vào "Species" — chưa làm, xem README.

KHÔNG tạo bảng mới. Mọi thứ ở đây chỉ để điền vào cột DB đã có sẵn.
"""
from __future__ import annotations
import polars as pl
from .config import PARQUET_DIR, PARQUET_FILES


def first_positive(*vals) -> float | None:
    """
    Trả giá trị dương đầu tiên trong danh sách nguồn, theo thứ tự ưu tiên.

    Coi 0 là "không có số liệu" — cân nặng, tuổi thọ và bậc dinh dưỡng đều không
    thể bằng 0, mà FishBase lẫn bản ETL cũ đều dùng 0 thay cho ô trống.
    """
    for v in vals:
        if v is not None and v > 0:
            return float(v)
    return None


def _agg_by_key(file_key: str, key_col: str, val_col: str, keys: set[int] | None,
                how: str = "max") -> dict[int, float]:
    """
    Gộp {key → giá trị} từ một parquet. Nhiều bảng FishBase có nhiều dòng mỗi loài
    (mỗi stock / mỗi giới tính), nên phải gộp chứ không lấy dòng đầu.
    """
    path = PARQUET_DIR / PARQUET_FILES[file_key]
    if not path.exists():
        print(f"  [sources] SKIP — {path} không tồn tại.")
        return {}

    schema = pl.read_parquet_schema(path)
    if key_col not in schema or val_col not in schema:
        print(f"  [sources] SKIP — {file_key} thiếu cột {key_col}/{val_col}.")
        return {}

    df = pl.read_parquet(path, columns=[key_col, val_col])
    df = df.filter(pl.col(val_col).is_not_null() & (pl.col(val_col) > 0))
    if keys is not None:
        df = df.filter(pl.col(key_col).is_in(list(keys)))
    df = df.group_by(key_col).agg(getattr(pl.col(val_col), how)())
    return {int(k): float(v) for k, v in zip(df[key_col], df[val_col])}


def weight_by_spec(spec_codes: set[int] | None = None) -> dict[int, float]:
    """popchar.Wmax — cân nặng lớn nhất ghi nhận được. TypeWeight toàn bộ là
    'total weight', cùng đơn vị và cùng nghĩa với species.Weight."""
    return _agg_by_key("popchar", "Speccode", "Wmax", spec_codes)


def age_by_spec(spec_codes: set[int] | None = None) -> dict[int, float]:
    """
    Tuổi lớn nhất ghi nhận được, gộp từ ba bảng theo thứ tự tin cậy giảm dần:
    popchar.tmax (quan sát trực tiếp) → popgrowth.tmax → estimate.AgeMax (suy ra).

    Lưu ý ngữ nghĩa: tmax/AgeMax là tuổi của cá thể già nhất từng ghi nhận, không
    hoàn toàn trùng khái niệm với LongevityWild, nhưng cùng trả lời "sống được bao lâu".
    """
    merged: dict[int, float] = {}
    for src in (_agg_by_key("estimate", "SpecCode", "AgeMax", spec_codes),
                _agg_by_key("popgrowth", "SpecCode", "tmax", spec_codes),
                _agg_by_key("popchar", "Speccode", "tmax", spec_codes)):
        merged.update(src)  # nguồn sau ghi đè nguồn trước → độ tin cậy cao thắng
    return merged


def troph_by_spec(spec_codes: set[int] | None = None) -> dict[int, float]:
    """estimate.Troph — chính là con số 'Trophic level' trên trang species của
    FishBase. Đúng 1 dòng mỗi loài, phủ 100%."""
    return _agg_by_key("estimate", "SpecCode", "Troph", spec_codes)
