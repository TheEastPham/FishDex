using System.Text;
using System.Text.Json;
using AquaHome.Domain.DTOs;
using AquaHome.Domain.Enums;
using AquaHome.Domain.Services.Interfaces;
using AquaHome.Domain.Settings;
using AquaHome.EFCore.Entity;
using AquaHome.EFCore.Repository.Interface;
using FishLover.Shared.Common;
using FishLover.Shared.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AquaHome.Domain.Services;

public class SnapshotService(
    IAquariumSnapshotRepository snapshotRepo,
    IAquariumSnapshotLikeRepository likeRepo,
    IAquariumRepository aquariumRepo,
    IAquariumMediaRepository mediaRepo,
    IFishDexClient fishDexClient,
    IStorageService storage,
    IOptions<StorageSettings> storageOptions,
    ICurrentUserSession currentUser,
    ILogger<SnapshotService> logger) : ISnapshotService
{
    private const int MaxSnapshotsPerAquarium = 5;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private string Env => storageOptions.Value.Environment;

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
            target.SnapshotData = JsonSerializer.Serialize(snapshotData, JsonOptions);
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
                SnapshotData = JsonSerializer.Serialize(snapshotData, JsonOptions),
            };

            await snapshotRepo.AddAsync(snapshot, ct);
            await snapshotRepo.SaveChangesAsync(ct);
        }

        return await ToDtoAsync(snapshot, snapshotData, likedByMe: false, ct);
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

        var result = new List<MySnapshotDto>(snapshots.Count);
        foreach (var s in snapshots)
        {
            // Chỉ cần aquariumName từ JSONB — không trả cả fish list cho trang quản lý / form chọn bể
            var name = TryGetAquariumName(s.SnapshotData) ?? s.Slug;
            var coverUrl = await ResolveCoverUrlAsync(s.CoverMediaId, ct);

            result.Add(new MySnapshotDto(
                s.Id, s.AquariumId, s.Slug, name, (WaterType)s.WaterType, (AquariumStyle)s.Style,
                s.FishSpeciesCount, s.LikeCount, coverUrl, s.CreatedAt, s.UpdatedAt));
        }
        return result;
    }

    private static string? TryGetAquariumName(string snapshotJson)
    {
        try
        {
            return JsonSerializer.Deserialize<SnapshotDataDto>(snapshotJson, JsonOptions)?.AquariumName;
        }
        catch
        {
            return null;
        }
    }

    public async Task<PagedResult<AquariumSnapshotDto>> GetGalleryAsync(
        int? waterType, int? style, string? contest, string sort, int page, int pageSize, CancellationToken ct = default)
    {
        var (items, total) = await snapshotRepo.GetGalleryAsync(waterType, style, contest, sort, page, pageSize, ct);

        var dtos = new List<AquariumSnapshotDto>(items.Count);
        foreach (var s in items)
            dtos.Add(await ToDtoAsync(s, snapshotData: null, likedByMe: false, ct));

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

        var snapshotData = JsonSerializer.Deserialize<SnapshotDataDto>(snapshot.SnapshotData, JsonOptions);
        if (snapshotData is not null)
            snapshotData = await RefreshFishImagesAsync(snapshotData, ct);

        var likedByMe = currentUser.IsAuthenticated
            && await likeRepo.ExistsAsync(snapshot.Id, currentUser.UserId, ct);

        return await ToDtoAsync(snapshot, snapshotData, likedByMe, ct);
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
        return media is null ? null : await storage.GetPresignedUrlAsync(media.ObjectKey(Env), ct);
    }

    /// <summary>
    /// Ảnh loài cá trong JSONB là presigned URL từ FishDex → hết hạn, phải ký lại mỗi lần đọc.
    /// Tiện thể refresh luôn tên loài để "chữa lành" snapshot cũ lỡ lưu fallback "Species #{code}"
    /// (do lookup thất bại lúc publish). 1 call batch cho toàn bộ specCode của snapshot.
    /// </summary>
    private async Task<SnapshotDataDto> RefreshFishImagesAsync(SnapshotDataDto data, CancellationToken ct)
    {
        if (data.Fish.Count == 0) return data;

        var specCodes = data.Fish.Select(f => f.SpecCode).Distinct().ToList();
        var summaries = (await fishDexClient.GetSpeciesSummariesAsync(specCodes, ct)).ToDictionary(s => s.SpecCode);

        var fish = data.Fish
            .Select(f => summaries.TryGetValue(f.SpecCode, out var s)
                ? f with
                {
                    ImageUrl = s.ImageUrl,
                    SpeciesName = string.IsNullOrWhiteSpace(s.SpeciesName) ? f.SpeciesName : s.SpeciesName,
                    CommonName = s.CommonName ?? f.CommonName,
                }
                : f)
            .ToList();

        return data with { Fish = fish };
    }

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
                summary?.ImageUrl,
                entry.Quantity,
                points));
        }

        return new SnapshotDataDto(
            aquarium.Name, currentUser.UserName, aquarium.LengthCm, aquarium.WidthCm, aquarium.HeightCm,
            aquarium.VolumeLiters, aquarium.Description, fish);
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

    private async Task<AquariumSnapshotDto> ToDtoAsync(AquariumSnapshot s, SnapshotDataDto? snapshotData, bool likedByMe, CancellationToken ct)
    {
        var coverImageUrl = await ResolveCoverUrlAsync(s.CoverMediaId, ct);
        return new AquariumSnapshotDto(
            s.Id, s.Slug, (WaterType)s.WaterType, (AquariumStyle)s.Style, s.LikeCount, s.FishSpeciesCount,
            s.AwardTierName, s.AwardTierLevel.HasValue ? (PrizeTierLevel)s.AwardTierLevel.Value : null,
            coverImageUrl, s.YoutubeVideoUrl, s.CreatedAt, s.UpdatedAt, snapshotData, likedByMe);
    }
}
