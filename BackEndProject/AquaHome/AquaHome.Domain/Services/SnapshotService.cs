using System.Text;
using System.Text.Json;
using AquaHome.Domain.DTOs;
using AquaHome.Domain.Enums;
using AquaHome.Domain.Services.Interfaces;
using AquaHome.EFCore.Entity;
using AquaHome.EFCore.Repository.Interface;
using FishLover.Shared.Common;
using FishLover.Shared.Services;
using Microsoft.Extensions.Logging;

namespace AquaHome.Domain.Services;

public class SnapshotService(
    IAquariumSnapshotRepository snapshotRepo,
    IAquariumSnapshotLikeRepository likeRepo,
    IAquariumRepository aquariumRepo,
    IFishDexClient fishDexClient,
    ICurrentUserSession currentUser,
    ILogger<SnapshotService> logger) : ISnapshotService
{
    private const int MaxSnapshotsPerAquarium = 5;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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

        // Max 5 snapshot/aquarium — khi đủ 5, auto-archive (IsActive=false) snapshot cũ nhất
        var active = await snapshotRepo.GetActiveByAquariumAsync(aquariumId, ct);
        if (active.Count >= MaxSnapshotsPerAquarium)
        {
            var oldest = active[0]; // đã ORDER BY CreatedAt ASC
            oldest.IsActive = false;
            logger.LogInformation("Auto-archived oldest snapshot {SnapshotId} for aquarium {AquariumId}", oldest.Id, aquariumId);
        }

        var slug = await GenerateUniqueSlugAsync(aquarium.Name, ct);

        var snapshot = new AquariumSnapshot
        {
            Id = Guid.NewGuid(),
            AquariumId = aquariumId,
            UserId = currentUser.UserId,
            Slug = slug,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            WaterType = aquarium.WaterType ?? 0,
            Style = aquarium.Style ?? 0,
            LikeCount = 0,
            FishSpeciesCount = snapshotData.Fish.Count,
            CoverImageUrl = request.CoverImageUrl,
            SnapshotData = JsonSerializer.Serialize(snapshotData, JsonOptions),
        };

        await snapshotRepo.AddAsync(snapshot, ct);
        await snapshotRepo.SaveChangesAsync(ct);

        return ToDto(snapshot, snapshotData, likedByMe: false);
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
        return snapshots.Select(s =>
        {
            // Chỉ cần aquariumName từ JSONB — không trả cả fish list cho form chọn bể
            var name = TryGetAquariumName(s.SnapshotData) ?? s.Slug;
            return new MySnapshotDto(
                s.Id, s.Slug, name, (WaterType)s.WaterType, (AquariumStyle)s.Style,
                s.FishSpeciesCount, s.LikeCount, s.CreatedAt);
        }).ToList();
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
        var dtos = items.Select(s => ToDto(s, snapshotData: null, likedByMe: false)).ToList();

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

        var likedByMe = currentUser.IsAuthenticated
            && await likeRepo.ExistsAsync(snapshot.Id, currentUser.UserId, ct);

        return ToDto(snapshot, snapshotData, likedByMe);
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

    private async Task<SnapshotDataDto> BuildSnapshotDataAsync(Aquarium aquarium, CancellationToken ct)
    {
        var fishEntries = await aquariumRepo.GetFishListAsync(aquarium.Id, ct);
        var specCodes = fishEntries.Select(f => f.SpecCode).Distinct().ToList();

        var summaries = (await fishDexClient.GetSpeciesSummariesAsync(specCodes, ct))
            .ToDictionary(s => s.SpecCode);

        var fish = new List<SnapshotFishDto>(fishEntries.Count);
        foreach (var entry in fishEntries)
        {
            var points = await fishDexClient.GetOccurrencesAsync(entry.SpecCode, ct);
            summaries.TryGetValue(entry.SpecCode, out var summary);

            fish.Add(new SnapshotFishDto(
                entry.SpecCode,
                summary?.SpeciesName ?? $"Species #{entry.SpecCode}",
                summary?.CommonName,
                summary?.ImageUrl,
                entry.Quantity,
                points));
        }

        return new SnapshotDataDto(
            aquarium.Name, aquarium.LengthCm, aquarium.WidthCm, aquarium.HeightCm,
            aquarium.VolumeLiters, aquarium.Description, fish);
    }

    private async Task<string> GenerateUniqueSlugAsync(string aquariumName, CancellationToken ct)
    {
        var baseSlug = Slugify(aquariumName);
        string slug;
        do
        {
            slug = $"{baseSlug}-{RandomSuffix(4)}";
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

    private static string RandomSuffix(int length)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        var buffer = new char[length];
        for (var i = 0; i < length; i++)
            buffer[i] = chars[Random.Shared.Next(chars.Length)];
        return new string(buffer);
    }

    private static AquariumSnapshotDto ToDto(AquariumSnapshot s, SnapshotDataDto? snapshotData, bool likedByMe) => new(
        s.Id, s.Slug, (WaterType)s.WaterType, (AquariumStyle)s.Style, s.LikeCount, s.FishSpeciesCount,
        s.ContestAward.HasValue ? (ContestAward)s.ContestAward.Value : null,
        s.CoverImageUrl, s.YoutubeVideoUrl, s.CreatedAt, snapshotData, likedByMe);
}
