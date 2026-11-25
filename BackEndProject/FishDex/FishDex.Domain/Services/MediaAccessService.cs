using FishDex.Domain.DTOs.Media;
using FishDex.Domain.Services.Interfaces;
using FishDex.Domain.Storage;
using FishDex.EFCore.Repository.Interface;
using FishLover.Storage;
using FishLover.Storage.MediaAccess;
using Microsoft.Extensions.DependencyInjection;

namespace FishDex.Domain.Services;

/// <summary>
/// Media-access cho bucket FishDex: chữ ký và thời hạn do <see cref="MediaAccessSigner"/> lo (dùng chung với
/// service khác), ở đây chỉ còn rule "key nào được công khai" của riêng FishDex.
/// </summary>
/// <remarks>
/// SAS ẩn danh mở được mọi key trong bucket, nên key do user upload (prefix <c>community/</c>, lại đoán được vì
/// specCode tăng dần) phải qua kiểm duyệt mới redeem được — xem <see cref="IsPublicCommunityImageAsync"/>.
/// </remarks>
public class MediaAccessService(MediaAccessSigner signer, IStorageService storage, IServiceScopeFactory scopeFactory) : IMediaAccessService
{
    /// <summary>Redirect được cache tới trước khi presigned URL hết hạn bấy nhiêu giây.</summary>
    private const int CacheMarginInSeconds = 60;

    public SharedMediaAccessSignatureDto? CreateSharedSignature(int? expiryInSeconds)
    {
        var signature = signer.CreateShared(expiryInSeconds);
        return signature is null ? null : new SharedMediaAccessSignatureDto(signature.Sas, signature.ExpiresAt);
    }

    public async Task<MediaAccessRedeemResult> RedeemAsync(string? path, string? shared, string? exp, string? sig, CancellationToken ct = default)
    {
        var verified = signer.Verify(path, shared, exp, sig);
        if (verified.Denial is { } denial)
        {
            return MediaAccessRedeemResult.Deny(denial);
        }

        var objectKey = verified.ObjectKey!;
        if (objectKey.StartsWith(FishDexObjectKeys.CommunityPrefix, StringComparison.Ordinal))
        {
            var isPublic = FishDexObjectKeys.TryParseCommunity(objectKey, out var specCode)
                           && await IsPublicCommunityImageAsync(specCode, objectKey, ct);
            if (!isPublic)
            {
                return MediaAccessRedeemResult.Deny(MediaAccessDenial.Invalid);
            }
        }

        // Presigned URL không bao giờ sống lâu hơn link sinh ra nó.
        var lifetime = (int)Math.Min(signer.Settings.RedirectExpiryInSeconds, verified.RemainingSeconds);
        var location = await storage.GetPresignedUrlAsync(objectKey, TimeSpan.FromSeconds(lifetime), ct);
        if (location is null)
        {
            return MediaAccessRedeemResult.Deny(MediaAccessDenial.NotConfigured);
        }

        return new MediaAccessRedeemResult(location, Math.Max(0, lifetime - CacheMarginInSeconds), null);
    }

    /// <summary>
    /// Ảnh community chỉ public khi loài đã được admin duyệt VÀ key đúng là ảnh bìa hiện tại của loài đó VÀ object
    /// thật trên storage không vượt trần dung lượng. Browser cache 302 nên mỗi ảnh chỉ tốn 1 query + 1 HEAD / ~14 phút.
    /// </summary>
    /// <remarks>
    /// Check dung lượng ở đây vì presigned PUT dùng lại được tới khi hết hạn: upload ảnh nhỏ, confirm, rồi PUT đè
    /// file lớn bằng chính URL đó — confirm đã qua nên không bắt được. Redeem là chỗ ảnh được phục vụ công khai.
    /// </remarks>
    private async Task<bool> IsPublicCommunityImageAsync(int specCode, string objectKey, CancellationToken ct)
    {
        // Service là singleton, repository dùng DbContext scoped → mở scope riêng cho lần tra này.
        await using var scope = scopeFactory.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<ICommunitySpeciesRepository>();
        var species = await repo.GetVerifiedByCodeAsync(specCode, ct);
        if (species?.ThumbnailObjectKey != objectKey)
        {
            return false;
        }

        var size = await storage.GetObjectSizeAsync(objectKey, ct);
        return size is not null && size <= FishDexObjectKeys.CommunityCoverMaxBytes;
    }
}
