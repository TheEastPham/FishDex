namespace AquaHome.Domain.Services.Interfaces;

public interface IYouTubeUploadService
{
    /// <summary>Download video từ R2 (objectKey) → upload lên YouTube channel dạng Unlisted. Trả về YouTubeVideoId.</summary>
    Task<string?> UploadUnlistedAsync(string objectKey, string title, string description, CancellationToken ct = default);

    /// <summary>Admin approve — set video Public, thêm vào playlist của contest (nếu có).</summary>
    Task SetPublicAsync(string youTubeVideoId, string? playlistId, CancellationToken ct = default);

    /// <summary>Admin reject — xóa video khỏi YouTube.</summary>
    Task DeleteVideoAsync(string youTubeVideoId, CancellationToken ct = default);

    /// <summary>Lấy toàn bộ videoId trong 1 playlist contest.</summary>
    Task<IReadOnlyList<string>> GetPlaylistVideoIdsAsync(string playlistId, CancellationToken ct = default);

    /// <summary>Batch lấy viewCount hiện tại cho danh sách videoId (dùng cho leaderboard sync).</summary>
    Task<IReadOnlyDictionary<string, long>> GetViewCountsAsync(IReadOnlyList<string> videoIds, CancellationToken ct = default);
}
