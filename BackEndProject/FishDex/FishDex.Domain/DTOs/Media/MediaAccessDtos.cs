namespace FishDex.Domain.DTOs.Media;

/// <summary>
/// SAS dùng chung cho mọi key: FE ghép <c>{gateway}/fishdex/v1/public/media-access?path={encodeURIComponent(key)}&amp;{Sas}</c>.
/// </summary>
public record SharedMediaAccessSignatureDto(string Sas, DateTimeOffset ExpiresAt);

public record CreateMediaAccessSignatureRequest(int? ExpiryInSeconds);

public enum MediaAccessDenial
{
    /// <summary>Link sai/bị sửa — 403 chung, không lộ check nào trượt.</summary>
    Invalid,

    /// <summary>Link thật nhưng hết hạn — FE xin SAS mới.</summary>
    Expired,

    /// <summary>Chưa cấu hình khoá ký hoặc storage — 503.</summary>
    NotConfigured,
}

/// <summary>Kết quả redeem: hoặc Location (302) hoặc lý do từ chối.</summary>
public record MediaAccessRedeemResult(string? Location, int CacheMaxAgeSeconds, MediaAccessDenial? Denial)
{
    public static MediaAccessRedeemResult Deny(MediaAccessDenial reason) => new(null, 0, reason);
}
