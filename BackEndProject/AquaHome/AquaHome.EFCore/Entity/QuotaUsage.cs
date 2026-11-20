namespace AquaHome.EFCore.Entity;

/// <summary>
/// Đếm lượt dùng theo ngày cho các quota dạng *PerDay (search/aiQa/imageSearch, lượt upload YouTube contest).
/// Composite PK (UserId, QuotaType, Day). Lưu DB thay vì in-memory để bền qua restart.
/// Row có UserId = Guid.Empty là bộ đếm toàn hệ thống (xem QuotaType.ContestYouTubeUploadPerDay).
/// </summary>
public class QuotaUsage
{
    public Guid UserId { get; set; }
    public int QuotaType { get; set; }
    public DateOnly Day { get; set; }
    public int Count { get; set; }
}
