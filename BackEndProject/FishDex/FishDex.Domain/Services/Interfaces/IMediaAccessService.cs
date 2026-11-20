using FishDex.Domain.DTOs.Media;

namespace FishDex.Domain.Services.Interfaces;

/// <summary>
/// Link media-access cho ảnh bucket system-image: URL ổn định (browser cache được) thay cho presigned URL
/// đổi mỗi lần gọi API. Response chỉ cần trả object key, FE ghép với SAS.
/// </summary>
public interface IMediaAccessService
{
    /// <summary>Null nếu chưa cấu hình khoá ký. <paramref name="expiryInSeconds"/> ngoài [1, Max] → ArgumentOutOfRangeException.</summary>
    SharedMediaAccessSignatureDto? CreateSharedSignature(int? expiryInSeconds);

    Task<MediaAccessRedeemResult> RedeemAsync(string? path, string? shared, string? exp, string? sig, CancellationToken ct = default);
}
