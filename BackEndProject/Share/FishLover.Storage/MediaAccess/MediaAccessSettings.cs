namespace FishLover.Storage.MediaAccess;

/// <summary>
/// Khoá ký + chính sách thời hạn cho link media-access (section <c>MediaAccess</c> của service phát link).
/// Port từ grapeseed-student-service, bỏ country/tenant vì FishLover không có khái niệm đó.
/// </summary>
/// <remarks>
/// Đổi <see cref="Key"/> = mọi link đã phát ra đều chết ngay. Để trống thì endpoint media-access
/// trả 503 (fail-closed, giống <c>InternalSettings:ApiKey</c>) — không ký bằng khoá rỗng.
/// </remarks>
public class MediaAccessSettings
{
    public const string SectionName = "MediaAccess";

    /// <summary>Khoá HMAC tối thiểu = kích thước output SHA-256.</summary>
    public const int MinKeyLengthInBytes = 32;

    /// <summary>Base64 HMAC secret. Sinh bằng: <c>openssl rand -base64 32</c>.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Thời hạn SAS khi client không tự chọn.</summary>
    public int DefaultExpiryInSeconds { get; set; } = 7200;

    /// <summary>Trần thời hạn SAS. SAS mở được mọi key trong bucket nên phải ngắn.</summary>
    public int MaxExpiryInSeconds { get; set; } = 7200;

    /// <summary>Thời hạn presigned R2 URL mà redeem redirect tới.</summary>
    public int RedirectExpiryInSeconds { get; set; } = 900;
}
