using FishLover.Storage.MediaAccess;

namespace FishDex.Domain.DTOs.Media;

/// <summary>
/// SAS dùng chung cho mọi key: FE ghép <c>{gateway}/fishdex/v1/public/media-access?path={encodeURIComponent(key)}&amp;{Sas}</c>.
/// </summary>
public record SharedMediaAccessSignatureDto(string Sas, DateTimeOffset ExpiresAt);

public record CreateMediaAccessSignatureRequest(int? ExpiryInSeconds);

/// <summary>Kết quả redeem: hoặc Location (302) hoặc lý do từ chối.</summary>
public record MediaAccessRedeemResult(string? Location, int CacheMaxAgeSeconds, MediaAccessDenial? Denial)
{
    public static MediaAccessRedeemResult Deny(MediaAccessDenial reason)
    {
        return new(null, 0, reason);
    }
}
