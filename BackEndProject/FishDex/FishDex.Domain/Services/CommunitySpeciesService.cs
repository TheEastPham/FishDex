using FishDex.Domain.DTOs.Species;
using FishDex.Domain.Services.Interfaces;
using FishDex.EFCore.Entity.Cache;
using FishDex.EFCore.Repository.Interface;
using FishLover.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FishDex.Domain.Services;

/// <summary>
/// Luồng community species (loài lai tạo không có trong FishBase) — submit + moderation.
/// Lưu thẳng vào SpeciesSnapshot (Community, SpecCode ≥ 500000); read path đọc SpeciesSnapshot
/// cho code community đã verified (xử lý ở SpeciesService).
/// </summary>
public class CommunitySpeciesService(
    ICommunitySpeciesRepository repo,
    IStorageService storage,
    ICurrentUserSession currentUser,
    ILogger<CommunitySpeciesService> logger) : ICommunitySpeciesService
{
    private const int MaxAllocationRetries = 3;
    private const long MaxImageUploadBytes = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedImageContentTypes = ["image/jpeg", "image/png", "image/webp"];

    public async Task<CommunitySpeciesDto> SubmitAsync(SubmitCommunitySpeciesRequest request, CancellationToken ct = default)
    {
        // Cấp SpecCode + insert, retry nếu 2 submit đồng thời trùng code (unique PK conflict).
        for (var attempt = 1; ; attempt++)
        {
            var specCode = await repo.GetNextCommunitySpecCodeAsync(ct);
            var snapshot = BuildSnapshot(specCode, request);
            await repo.AddAsync(snapshot, ct);

            try
            {
                await repo.SaveChangesAsync(ct);
                logger.LogInformation("Community species submitted: SpecCode {SpecCode} by {UserId}", specCode, currentUser.UserId);
                return await ToDtoAsync(snapshot, ct);
            }
            catch (DbUpdateException) when (attempt < MaxAllocationRetries)
            {
                logger.LogWarning("SpecCode {SpecCode} conflict on submit — reallocating (attempt {Attempt})", specCode, attempt);
                // Bỏ entity tracked lỗi để vòng sau cấp code mới
                repo.Detach(snapshot);
            }
        }
    }

    public async Task<IReadOnlyList<CommunitySpeciesDto>> GetMineAsync(CancellationToken ct = default)
    {
        var items = await repo.GetByContributorAsync(currentUser.UserId, ct);
        return await MapManyAsync(items, ct);
    }

    public async Task<IReadOnlyList<CommunitySpeciesDto>> GetPendingAsync(CancellationToken ct = default)
    {
        var items = await repo.GetPendingAsync(ct);
        return await MapManyAsync(items, ct);
    }

    public async Task<bool> VerifyAsync(int specCode, CommunitySpeciesKind? kind, CancellationToken ct = default)
    {
        var snapshot = await repo.GetCommunityByCodeAsync(specCode, ct);
        if (snapshot is null) return false;

        snapshot.IsVerified = true;
        snapshot.RejectionReason = null;
        snapshot.ReviewedBy = currentUser.UserId;
        snapshot.Kind = kind ?? snapshot.SuggestedKind;
        await repo.SaveChangesAsync(ct);
        logger.LogInformation("Community species {SpecCode} verified by {UserId} as {Kind}", specCode, currentUser.UserId, snapshot.Kind);
        return true;
    }

    public async Task<bool> RejectAsync(int specCode, string reason, CancellationToken ct = default)
    {
        var snapshot = await repo.GetCommunityByCodeAsync(specCode, ct);
        if (snapshot is null) return false;

        snapshot.IsVerified = false;
        snapshot.RejectionReason = reason;
        snapshot.ReviewedBy = currentUser.UserId;
        await repo.SaveChangesAsync(ct);
        logger.LogInformation("Community species {SpecCode} rejected by {UserId}", specCode, currentUser.UserId);
        return true;
    }

    public async Task<CommunityImageUploadResultDto?> RequestImageUploadAsync(
        int specCode, string fileName, string contentType, CancellationToken ct = default)
    {
        if (!AllowedImageContentTypes.Contains(contentType)) return null;

        var snapshot = await repo.GetCommunityByCodeAsync(specCode, ct);
        if (snapshot is null || snapshot.ContributedBy != currentUser.UserId) return null;

        var ext = Path.GetExtension(fileName);
        var objectKey = $"community/{specCode}/cover{ext}";

        var uploadUrl = await storage.GeneratePresignedPutUrlAsync(objectKey, contentType, MaxImageUploadBytes, ct);
        if (uploadUrl is null) return null;

        snapshot.ThumbnailObjectKey = objectKey;
        await repo.SaveChangesAsync(ct);

        return new CommunityImageUploadResultDto(uploadUrl, objectKey);
    }

    // ─────────────────────────────────────────────────────────────────────────

    private SpeciesSnapshot BuildSnapshot(int specCode, SubmitCommunitySpeciesRequest r) => new()
    {
        SpecCode         = specCode,
        DataSource       = SnapshotDataSource.Community,
        IsVerified       = false,
        SpeciesName      = r.SpeciesName.Trim(),
        CommonName       = r.CommonName?.Trim(),
        FamilyName       = r.FamilyName?.Trim(),
        GenusName        = r.GenusName?.Trim(),
        SuggestedKind    = r.SuggestedKind,
        WaterType        = r.WaterType,
        TempMin          = r.TempMin,
        TempMax          = r.TempMax,
        PhMin            = r.PhMin,
        PhMax            = r.PhMax,
        DhMin            = r.DhMin,
        DhMax            = r.DhMax,
        Length           = r.Length,
        LongevityCaptive = r.LongevityCaptive,
        FeedingType      = r.FeedingType,
        FeedingPosition  = r.FeedingPosition,
        ActivityPattern  = r.ActivityPattern,
        RequiresLiveFood = r.RequiresLiveFood,
        Aggressiveness   = r.Aggressiveness,
        FinNippingRisk   = r.FinNippingRisk,
        JumpingRisk      = r.JumpingRisk,
        CareLevel        = r.CareLevel,
        MinTankLiters    = r.MinTankLiters,
        ContributedBy    = currentUser.UserId,
        PopulatedFrom    = SnapshotPopulatedFrom.Manual,
        PopulatedAt      = DateTime.UtcNow,
    };

    private async Task<IReadOnlyList<CommunitySpeciesDto>> MapManyAsync(IReadOnlyList<SpeciesSnapshot> items, CancellationToken ct)
    {
        var result = new List<CommunitySpeciesDto>(items.Count);
        foreach (var s in items)
            result.Add(await ToDtoAsync(s, ct));
        return result;
    }

    private async Task<CommunitySpeciesDto> ToDtoAsync(SpeciesSnapshot s, CancellationToken ct)
    {
        var imageUrl = s.ThumbnailObjectKey is not null
            ? await storage.GetPresignedUrlAsync(s.ThumbnailObjectKey, ct)
            : null;

        return new CommunitySpeciesDto(
            s.SpecCode, s.SpeciesName, s.CommonName, s.FamilyName, s.GenusName,
            s.WaterType, s.IsVerified, s.RejectionReason, s.ContributedBy, imageUrl, s.PopulatedAt,
            s.SuggestedKind, s.Kind);
    }
}
