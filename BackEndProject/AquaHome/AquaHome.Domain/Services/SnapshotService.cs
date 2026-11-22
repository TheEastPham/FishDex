using System.Text;
using System.Text.Json;
using AquaHome.Domain.DTOs;
using AquaHome.Domain.Enums;
using AquaHome.Domain.Exceptions;
using AquaHome.Domain.Extensions;
using AquaHome.Domain.Services.Interfaces;
using AquaHome.Domain.Storage;
using AquaHome.EFCore.Entity;
using AquaHome.EFCore.Repository.Interface;
using FishLover.Shared.Common;
using FishLover.Shared.Services;
using FishLover.Storage;
using Microsoft.Extensions.Logging;

namespace AquaHome.Domain.Services;

public class SnapshotService(
    IAquariumSnapshotRepository snapshotRepo,
    IAquariumSnapshotLikeRepository likeRepo,
    IAquariumRepository aquariumRepo,
    IAquariumMediaRepository mediaRepo,
    IFishDexClient fishDexClient,
    IStorageService storage,
    AquaHomeObjectKeys keys,
    ICurrentUserSession currentUser,
    ILogger<SnapshotService> logger) : ISnapshotService
{
    private const int MaxSnapshotsPerAquarium = 5;

    public async Task<SnapshotPreviewDto?> PreviewAsync(Guid aquariumId, CancellationToken ct = default)
    {
        var aquarium = await aquariumRepo.GetByIdAndUserAsync(aquariumId, currentUser.UserId, ct);
        if (aquarium is null) return null;

        var snapshotData = await BuildSnapshotDataAsync(aquarium, ct);

        return new SnapshotPreviewDto(
            aquarium.WaterType.HasValue ? (WaterType)aquarium.WaterType.Value : null,
            aquarium.Style.HasValue ? (AquariumStyle)aquarium.Style.Value : null,
            snapshotData.Fish.Count,
            snapshotData);
    }

    public async Task<AquariumSnapshotDto?> PublishAsync(Guid aquariumId, PublishSnapshotRequest request, CancellationToken ct = default)
    {
        var aquarium = await aquariumRepo.GetByIdAndUserAsync(aquariumId, currentUser.UserId, ct);
        if (aquarium is null) return null;

        var snapshotData = await BuildSnapshotDataAsync(aquarium, ct);
        var now = DateTime.UtcNow;

        AquariumSnapshot snapshot;

        if (request.TargetSnapshotId.HasValue)
        {
            // Ghi đè snapshot đang active — giữ nguyên Slug/Id/LikeCount/AwardTier, chỉ refresh nội dung
            var target = await snapshotRepo.GetByIdAsync(request.TargetSnapshotId.Value, ct);
            if (target is null || target.AquariumId != aquariumId || target.UserId != currentUser.UserId || !target.IsActive)
            {
                logger.LogWarning("TargetSnapshotId {SnapshotId} không hợp lệ để ghi đè cho aquarium {AquariumId}", request.TargetSnapshotId, aquariumId);
                return null;
            }

            target.WaterType = aquarium.WaterType ?? 0;
            target.Style = aquarium.Style ?? 0;
            target.FishSpeciesCount = snapshotData.Fish.Count;
            target.CoverMediaId = request.CoverMediaId;
            target.SnapshotData = SnapshotDataReader.Serialize(snapshotData);
            target.UpdatedAt = now;

            await snapshotRepo.SaveChangesAsync(ct);
            snapshot = target;
        }
        else
        {
            // Max 5 snapshot/aquarium — khi đủ 5, auto-archive (IsActive=false) snapshot cũ nhất
            var active = await snapshotRepo.GetActiveByAquariumAsync(aquariumId, ct);
            if (active.Count >= MaxSnapshotsPerAquarium)
            {
                var oldest = active[0]; // đã ORDER BY CreatedAt ASC
                oldest.IsActive = false;
                logger.LogInformation("Auto-archived oldest snapshot {SnapshotId} for aquarium {AquariumId}", oldest.Id, aquariumId);
            }

            var slug = await GenerateUniqueSlugAsync(aquarium.Name, ct);

            snapshot = new AquariumSnapshot
            {
                Id = Guid.NewGuid(),
                AquariumId = aquariumId,
                UserId = currentUser.UserId,
                Slug = slug,
                CreatedAt = now,
                UpdatedAt = now,
                IsActive = true,
                WaterType = aquarium.WaterType ?? 0,
                Style = aquarium.Style ?? 0,
                LikeCount = 0,
                FishSpeciesCount = snapshotData.Fish.Count,
                CoverMediaId = request.CoverMediaId,
                SnapshotData = SnapshotDataReader.Serialize(snapshotData),
            };

            await snapshotRepo.AddAsync(snapshot, ct);
            await snapshotRepo.SaveChangesAsync(ct);
        }

        return ToDto(snapshot, snapshotData, likedByMe: false, await ResolveCoverUrlAsync(snapshot.CoverMediaId, ct));
    }

    public async Task<bool> UnpublishAsync(Guid snapshotId, CancellationToken ct = default)
    {
        var snapshot = await snapshotRepo.GetByIdAsync(snapshotId, ct);
        if (snapshot is null || snapshot.UserId != currentUser.UserId) return false;

        snapshot.IsActive = false;
        await snapshotRepo.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<MySnapshotDto>> GetMineAsync(CancellationToken ct = default)
    {
        var snapshots = await snapshotRepo.GetActiveByUserAsync(currentUser.UserId, ct);

        var covers = await ResolveCoverUrlsAsync(snapshots.Select(s => s.CoverMediaId), ct);

        var result = new List<MySnapshotDto>(snapshots.Count);
        foreach (var s in snapshots)
        {
            // Chỉ cần aquariumName từ JSONB — không trả cả fish list cho trang quản lý / form chọn bể
            var name = TryGetAquariumName(s.SnapshotData) ?? s.Slug;
            var coverUrl = CoverOf(covers, s.CoverMediaId);

            result.Add(new MySnapshotDto(
                s.Id, s.AquariumId, s.Slug, name, (WaterType)s.WaterType, (AquariumStyle)s.Style,
                s.FishSpeciesCount, s.LikeCount, coverUrl, s.CreatedAt, s.UpdatedAt));
        }
        return result;
    }

    private static string? TryGetAquariumName(string snapshotJson)
        => SnapshotDataReader.TryGetAquariumName(snapshotJson);

    public async Task<PagedResult<AquariumSnapshotDto>> GetGalleryAsync(
        int? waterType, int? style, string? contest, string sort, int page, int pageSize, CancellationToken ct = default)
    {
        var (items, total) = await snapshotRepo.GetGalleryAsync(waterType, style, contest, sort, page, pageSize, ct);

        var covers = await ResolveCoverUrlsAsync(items.Select(s => s.CoverMediaId), ct);

        var dtos = new List<AquariumSnapshotDto>(items.Count);
        foreach (var s in items)
            dtos.Add(ToDto(s, snapshotData: null, likedByMe: false, CoverOf(covers, s.CoverMediaId)));

        return new PagedResult<AquariumSnapshotDto>
        {
            Items = dtos,
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
        };
    }

    public async Task<AquariumSnapshotDto?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var snapshot = await snapshotRepo.GetBySlugAsync(slug, ct);
        if (snapshot is null) return null;

        // Cùng một đường đọc với ContestService: snapshot sai schema → null (trang vẫn mở, không 500).
        var snapshotData = SnapshotDataReader.TryRead(snapshot.SnapshotData);
        if (snapshotData is null)
            logger.LogWarning("Snapshot {SnapshotId} có SnapshotData không đọc được", snapshot.Id);
        else if (NeedsHealing(snapshotData))
        {
            var healed = await HealSnapshotDataAsync(snapshotData, ct);
            if (healed is not null)
            {
                // Ghi lại để lần xem sau khỏi gọi FishDex. Không đụng UpdatedAt — nội dung bể không đổi.
                snapshotData = healed;
                snapshot.SnapshotData = SnapshotDataReader.Serialize(healed);
                await snapshotRepo.SaveChangesAsync(ct);
            }
        }

        var likedByMe = currentUser.IsAuthenticated
            && await likeRepo.ExistsAsync(snapshot.Id, currentUser.UserId, ct);

        return ToDto(snapshot, snapshotData, likedByMe, await ResolveCoverUrlAsync(snapshot.CoverMediaId, ct));
    }

    public async Task<bool> LikeAsync(Guid snapshotId, CancellationToken ct = default)
    {
        var snapshot = await snapshotRepo.GetByIdAsync(snapshotId, ct);
        if (snapshot is null || !snapshot.IsActive) return false;

        if (await likeRepo.ExistsAsync(snapshotId, currentUser.UserId, ct))
            return true; // idempotent — đã like rồi

        await likeRepo.AddAsync(snapshotId, currentUser.UserId, ct);
        return true;
    }

    public async Task<bool> UnlikeAsync(Guid snapshotId, CancellationToken ct = default)
        => await likeRepo.RemoveAsync(snapshotId, currentUser.UserId, ct);

    /// <summary>Ký lại presigned URL mỗi lần đọc — không lưu URL cố định vì sẽ hết hạn sau PresignedUrlExpiryMinutes.</summary>
    private async Task<string?> ResolveCoverUrlAsync(Guid? coverMediaId, CancellationToken ct)
    {
        if (coverMediaId is null) return null;

        var media = await mediaRepo.GetByIdAsync(coverMediaId.Value, ct);
        return media is null ? null : await storage.GetPresignedUrlAsync(keys.AquariumMedia(media), ct);
    }

    /// <summary>Batch: 1 query media cho cả trang thay vì 1 query/item. Media đã bị xoá → không có entry (cover = null).</summary>
    private async Task<IReadOnlyDictionary<Guid, string>> ResolveCoverUrlsAsync(IEnumerable<Guid?> coverMediaIds, CancellationToken ct)
    {
        var ids = coverMediaIds.OfType<Guid>().Distinct().ToList();
        var urls = new Dictionary<Guid, string>(ids.Count);
        if (ids.Count == 0) return urls;

        foreach (var media in await mediaRepo.GetByIdsAsync(ids, ct))
            urls[media.Id] = await storage.GetPresignedUrlAsync(keys.AquariumMedia(media), ct);
        return urls;
    }

    private static string? CoverOf(IReadOnlyDictionary<Guid, string> covers, Guid? coverMediaId)
    {
        return coverMediaId is { } id && covers.TryGetValue(id, out var url) ? url : null;
    }

    private const string FallbackSpeciesNamePrefix = "Species #";

    /// <summary>
    /// JSONB bản mới lưu ImageKey (bất biến) nên đọc thẳng, không gọi FishDex. Chỉ cần chữa khi:
    /// Version &lt; 2 (snapshot cũ lưu presigned URL đã chết, hoặc lúc publish FishDex chưa trả key — xem
    /// <see cref="ResolveVersion"/>), hoặc lỡ lưu tên fallback "Species #{code}" do lookup thất bại lúc publish.
    /// </summary>
    private static bool NeedsHealing(SnapshotDataDto data)
        => data.Fish.Count > 0
           && (data.Version < SnapshotDataDto.CurrentVersion
               || data.Fish.Any(f => f.SpeciesName.StartsWith(FallbackSpeciesNamePrefix, StringComparison.Ordinal)));

    /// <summary>
    /// 1 call batch lấy ImageKey + tên loài cho toàn bộ specCode. Null nếu FishDex không trả gì
    /// (lỗi mạng…) — khi đó giữ nguyên bản cũ, không ghi đè, lần xem sau thử lại.
    /// </summary>
    private async Task<SnapshotDataDto?> HealSnapshotDataAsync(SnapshotDataDto data, CancellationToken ct)
    {
        var specCodes = data.Fish.Select(f => f.SpecCode).Distinct().ToList();
        Dictionary<int, SpeciesSummaryDto> summaries;
        try
        {
            summaries = (await fishDexClient.GetSpeciesSummariesAsync(specCodes, ct)).ToDictionary(s => s.SpecCode);
        }
        catch (FishDexUnavailableException)
        {
            return null; // đã log ở client; trang xem snapshot vẫn hiện bản cũ chứ không đáng 503
        }
        if (summaries.Count == 0) return null;

        var fish = data.Fish
            .Select(f => summaries.TryGetValue(f.SpecCode, out var s)
                ? f with
                {
                    ImageKey = s.ImageKey ?? f.ImageKey,
                    SpeciesName = string.IsNullOrWhiteSpace(s.SpeciesName) ? f.SpeciesName : s.SpeciesName,
                    CommonName = s.CommonName ?? f.CommonName,
                }
                : f)
            .ToList();

        return data with { Fish = fish, Version = ResolveVersion(fish, data.Version) };
    }

    /// <summary>
    /// Chỉ chốt bản mới khi FishDex thật sự trả về key. FishDex còn bản cũ (chưa có ImageKey — vd AquaHome
    /// deploy lên trước) thì mọi key đều null: giữ Version cũ để lần xem sau chữa tiếp, thay vì chốt vĩnh viễn
    /// một snapshot không ảnh. Đổi lại, snapshot mà mọi loài đều không có ảnh sẽ gọi FishDex mỗi lần xem
    /// (đúng như trước khi có media-access).
    /// </summary>
    private static int ResolveVersion(IReadOnlyList<SnapshotFishDto> fish, int fallback)
        => fish.Count == 0 || fish.Any(f => f.ImageKey is not null) ? SnapshotDataDto.CurrentVersion : fallback;

    private async Task<SnapshotDataDto> BuildSnapshotDataAsync(Aquarium aquarium, CancellationToken ct)
    {
        var fishEntries = await aquariumRepo.GetFishListAsync(aquarium.Id, ct);
        var specCodes = fishEntries.Select(f => f.SpecCode).Distinct().ToList();

        // 2 batch call thay vì 1 + N: summaries (1) + distributions (1)
        var summaries = (await fishDexClient.GetSpeciesSummariesAsync(specCodes, ct))
            .ToDictionary(s => s.SpecCode);
        var distributions = await fishDexClient.GetDistributionsAsync(specCodes, ct);

        var fish = new List<SnapshotFishDto>(fishEntries.Count);
        foreach (var entry in fishEntries)
        {
            summaries.TryGetValue(entry.SpecCode, out var summary);
            var points = distributions.TryGetValue(entry.SpecCode, out var p) ? p : [];

            fish.Add(new SnapshotFishDto(
                entry.SpecCode,
                summary?.SpeciesName ?? $"Species #{entry.SpecCode}",
                summary?.CommonName,
                summary?.ImageKey,
                entry.Quantity,
                points));
        }

        return new SnapshotDataDto(
            aquarium.Name, currentUser.UserName, aquarium.LengthCm, aquarium.WidthCm, aquarium.HeightCm,
            aquarium.VolumeLiters, aquarium.Description, fish, ResolveVersion(fish, fallback: 0));
    }

    /// <summary>Slug rõ ràng: {tên bể}-{nickname user}. Trùng (vd 2 bể cùng tên) thì thêm hậu tố số.</summary>
    private async Task<string> GenerateUniqueSlugAsync(string aquariumName, CancellationToken ct)
    {
        var baseSlug = Slugify(aquariumName);
        var nicknameSlug = Slugify(currentUser.UserName ?? string.Empty);
        var candidate = string.IsNullOrEmpty(nicknameSlug) ? baseSlug : $"{baseSlug}-{nicknameSlug}";

        if (!await snapshotRepo.SlugExistsAsync(candidate, ct))
            return candidate;

        var suffix = 2;
        string slug;
        do
        {
            slug = $"{candidate}-{suffix}";
            suffix++;
        } while (await snapshotRepo.SlugExistsAsync(slug, ct));

        return slug;
    }

    private static string Slugify(string name)
    {
        var normalized = name.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            var category = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        var ascii = sb.ToString().Normalize(NormalizationForm.FormC);
        var result = new StringBuilder();
        var lastWasDash = false;
        foreach (var c in ascii.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c) && c < 128)
            {
                result.Append(c);
                lastWasDash = false;
            }
            else if (!lastWasDash && result.Length > 0)
            {
                result.Append('-');
                lastWasDash = true;
            }
        }

        var slug = result.ToString().Trim('-');
        return string.IsNullOrEmpty(slug) ? "aquarium" : slug;
    }

    private static AquariumSnapshotDto ToDto(AquariumSnapshot s, SnapshotDataDto? snapshotData, bool likedByMe, string? coverImageUrl)
    {
        return new AquariumSnapshotDto(
            s.Id, s.Slug, (WaterType)s.WaterType, (AquariumStyle)s.Style, s.LikeCount, s.FishSpeciesCount,
            s.AwardTierName, s.AwardTierLevel.HasValue ? (PrizeTierLevel)s.AwardTierLevel.Value : null,
            coverImageUrl, s.YoutubeVideoUrl, s.CreatedAt, s.UpdatedAt, snapshotData, likedByMe);
    }
}
