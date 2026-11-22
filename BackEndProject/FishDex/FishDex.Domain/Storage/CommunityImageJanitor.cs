using FishDex.EFCore.Repository.Interface;
using FishLover.Storage;
using Microsoft.Extensions.Logging;

namespace FishDex.Domain.Storage;

/// <summary>Kết quả một lượt dọn: số object đã quét, số object mồ côi đã xoá, số ảnh bìa quá cỡ đã gỡ.</summary>
public sealed record CommunityImageCleanupResult(int Scanned, int DeletedOrphans, int DetachedOversized);

/// <summary>
/// Dọn vùng <c>community/</c> của bucket FishDex — lớp cuối cùng sau confirm và redeem, cho những gì hai lớp đó không xoá được:
/// <list type="bullet">
///   <item><b>Object mồ côi</b>: PUT lên rồi không bao giờ confirm, nên không loài nào trỏ tới. Chỉ xoá khi đã cũ hơn
///         <see cref="OrphanGracePeriod"/> để không cắt ngang người đang upload (URL PUT sống 15 phút).</item>
///   <item><b>Ảnh bìa quá cỡ</b>: PUT đè bằng URL còn hạn sau khi đã confirm. Redeem đã không phục vụ nó, ở đây xoá hẳn
///         và gỡ khỏi loài để nó không chiếm chỗ trên R2.</item>
/// </list>
/// Chạy định kỳ bởi <c>CommunityImageCleanupService</c>; gọi tay được để test.
/// </summary>
public sealed class CommunityImageJanitor(
    ICommunitySpeciesRepository repo,
    IStorageService storage,
    TimeProvider time,
    ILogger<CommunityImageJanitor> logger)
{
    public static readonly TimeSpan OrphanGracePeriod = TimeSpan.FromHours(1);

    public async Task<CommunityImageCleanupResult> RunAsync(CancellationToken ct = default)
    {
        if (!storage.IsConfigured)
        {
            return new CommunityImageCleanupResult(0, 0, 0);
        }

        // Liệt kê storage TRƯỚC rồi mới đọc DB: ảnh được confirm giữa hai bước chỉ có thể nằm ở DB, không bị coi là mồ côi.
        var objects = await storage.ListAsync(FishDexObjectKeys.CommunityPrefix, ct);
        var inUse = await repo.GetCommunityThumbnailKeysAsync(ct);
        var cutoff = time.GetUtcNow() - OrphanGracePeriod;

        var deleted = 0;
        var detached = 0;
        foreach (var obj in objects)
        {
            if (inUse.TryGetValue(obj.Key, out var specCode))
            {
                if (obj.Size > FishDexObjectKeys.CommunityCoverMaxBytes)
                {
                    await DetachOversizedAsync(specCode, obj, ct);
                    detached++;
                }
            }
            else if (obj.LastModified < cutoff)
            {
                await storage.DeleteAsync(obj.Key, ct);
                deleted++;
            }
        }

        if (deleted > 0 || detached > 0)
        {
            logger.LogWarning("Community image cleanup: scanned {Scanned}, deleted {Deleted} orphan(s), detached {Detached} oversized cover(s)", objects.Count, deleted, detached);
        }

        return new CommunityImageCleanupResult(objects.Count, deleted, detached);
    }

    private async Task DetachOversizedAsync(int specCode, StoredObjectInfo obj, CancellationToken ct)
    {
        await storage.DeleteAsync(obj.Key, ct);

        var snapshot = await repo.GetCommunityByCodeAsync(specCode, ct);
        if (snapshot?.ThumbnailObjectKey == obj.Key)
        {
            snapshot.ThumbnailObjectKey = null;
            await repo.SaveChangesAsync(ct);
        }

        logger.LogWarning("Community cover {Key} ({Bytes} bytes) exceeded {Max} after confirm — deleted and detached from SpecCode {SpecCode}", obj.Key, obj.Size, FishDexObjectKeys.CommunityCoverMaxBytes, specCode);
    }
}
