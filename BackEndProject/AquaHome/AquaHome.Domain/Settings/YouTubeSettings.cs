namespace AquaHome.Domain.Settings;

public class YouTubeSettings
{
    public const string SectionName = "ExternalServices:YouTube";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>Refresh token của channel owner FishLover — lấy 1 lần qua OAuth2 web-server flow.</summary>
    public string RefreshToken { get; set; } = string.Empty;

    public string ChannelId { get; set; } = string.Empty;

    /// <summary>
    /// Trần số video contest upload lên YouTube mỗi ngày (giờ Pacific — lúc quota Google reset), toàn hệ thống.
    /// Mỗi lần upload ≈ 1.600/10.000 unit; 5 lần chừa ~2.000 unit cho approve, xoá video và sync lượt xem.
    /// Google cấp thêm quota thì nâng số này.
    /// </summary>
    public int DailyUploadLimit { get; set; } = 5;
}
