namespace AquaHome.Domain.Exceptions;

/// <summary>
/// YouTube từ chối upload vì kênh hết quota / hết lượt upload trong ngày. Lỗi phía hệ thống chứ không phải
/// lỗi bài dự thi — ContestService giữ bài ở Pending để user xác nhận lại hôm sau thay vì từ chối bài.
/// </summary>
public class YouTubeQuotaExceededException(string reason)
    : Exception($"YouTube quota exceeded ({reason}).")
{
    public string Reason { get; } = reason;
}
