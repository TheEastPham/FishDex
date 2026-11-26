namespace AquaHome.Common.Enums;

/// <summary>
/// Trạng thái bài dự thi. Số đã lưu trong DB và FE so theo số — thêm trạng thái thì thêm số mới, không đổi số cũ.
/// Luật chuyển giữa các trạng thái nằm ở <c>AquaHome.Domain.Contests.ContestEntryLifecycle</c>.
/// </summary>
public enum ContestEntryStatus
{
    /// <summary>Đã nộp, chờ người dự thi tải video lên và xác nhận.</summary>
    Pending = 0,

    /// <summary>Đã xác nhận (hoặc đang kiểm) file, nằm trong hàng chờ để Worker đăng lên YouTube. Trước đây tên Validating.</summary>
    Queued = 1,

    /// <summary>Đã lên YouTube ở chế độ unlisted, chờ ban tổ chức duyệt.</summary>
    UploadedDraft = 2,

    /// <summary>Ban tổ chức đã duyệt: video công khai, lên bảng xếp hạng, được xét giải.</summary>
    Published = 3,

    /// <summary>Ban tổ chức từ chối vì nội dung.</summary>
    Rejected = 4,

    /// <summary>Hệ thống không đưa được video lên (file thiếu/lệch, YouTube không nhận, để quá lâu). Người dự thi nộp lại được.</summary>
    Failed = 5,
}
