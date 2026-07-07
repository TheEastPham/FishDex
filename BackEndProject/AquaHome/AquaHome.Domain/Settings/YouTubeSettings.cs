namespace AquaHome.Domain.Settings;

public class YouTubeSettings
{
    public const string SectionName = "ExternalServices:YouTube";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>Refresh token của channel owner FishLover — lấy 1 lần qua OAuth2 web-server flow.</summary>
    public string RefreshToken { get; set; } = string.Empty;

    public string ChannelId { get; set; } = string.Empty;
}
