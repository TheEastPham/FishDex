using FishDex.Domain.DTOs.Species;
using FishDex.Domain.Services.Interfaces;
using FishDex.Domain.Storage;
using FishDex.EFCore.Entity.Cache;
using FishDex.EFCore.Repository.Interface;
using FishLover.Shared.Services;
using FishLover.Storage;
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

    /// <summary>Số loài chưa duyệt (chờ + bị từ chối) mỗi user được giữ cùng lúc.</summary>
    private const int MaxUnverifiedPerUser = 5;
    // Đuôi file suy từ content-type (đã ký vào presigned PUT), không lấy theo tên file client gửi.
    private static readonly Dictionary<string, string> ImageExtensions = new()
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
    };

    /// <summary>
    /// Ngưỡng 0.4 chọn theo phép thử thực tế: "Betta splendens" so với "Beta splenden"
    /// (thiếu chữ, sai chính tả) cho 0.667, tức lỗi gõ thường gặp vẫn lọt lưới ở mức này,
    /// trong khi hai loài khác hẳn tên thì rơi xuống dưới ngưỡng.
    /// </summary>
    private const double SimilarityThreshold = 0.4;

    public async Task<IReadOnlyList<SimilarSpeciesDto>> FindSimilarAsync(string speciesName, CancellationToken ct = default)
    {
        var matches = await repo.FindSimilarNamesAsync(speciesName, SimilarityThreshold, limit: 5, ct);

        return matches
            .Select(m => new SimilarSpeciesDto(
                m.SpecCode,
                m.SpeciesName,
                m.Source switch
                {
                    SimilarNameSource.FishDex   => SimilarSpeciesOutcome.AlreadyInFishDex,
                    SimilarNameSource.FishBase  => SimilarSpeciesOutcome.NeedsMigration,
                    _                           => SimilarSpeciesOutcome.AlreadySubmitted,
                },
                Math.Round(m.Score, 3)))
            .ToList();
    }

    public async Task<SubmitCommunitySpeciesResult> SubmitAsync(SubmitCommunitySpeciesRequest request, CancellationToken ct = default)
    {
        // Mỗi loài = 1 key upload ảnh. Không có trần thì 1 tài khoản tạo hàng trăm loài nháp để chứa file trên R2.
        if (await repo.CountUnverifiedByContributorAsync(currentUser.UserId, ct) >= MaxUnverifiedPerUser)
        {
            return new SubmitCommunitySpeciesResult(SubmitCommunitySpeciesOutcome.PendingLimitReached);
        }

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
                return new SubmitCommunitySpeciesResult(SubmitCommunitySpeciesOutcome.Created, await ToDtoAsync(snapshot, ct));
            }
            catch (DbUpdateException) when (attempt < MaxAllocationRetries)
            {
                logger.LogWarning("SpecCode {SpecCode} conflict on submit — reallocating (attempt {Attempt})", specCode, attempt);
                // Bỏ entity tracked lỗi để vòng sau cấp code mới
                repo.Detach(snapshot);
            }
        }
    }

    public async Task<UpdateCommunitySpeciesResult> UpdateAsync(int specCode, SubmitCommunitySpeciesRequest r, CancellationToken ct = default)
    {
        var snapshot = await repo.GetCommunityByCodeAsync(specCode, ct);
        if (snapshot is null || snapshot.ContributedBy != currentUser.UserId)
            return new UpdateCommunitySpeciesResult(UpdateCommunitySpeciesOutcome.NotFound);

        if (snapshot.IsVerified || snapshot.RejectionReason != null)
            return new UpdateCommunitySpeciesResult(UpdateCommunitySpeciesOutcome.NotPending);

        ApplyRequest(snapshot, r);
        await repo.SaveChangesAsync(ct);
        logger.LogInformation("Community species {SpecCode} updated by {UserId}", specCode, currentUser.UserId);
        return new UpdateCommunitySpeciesResult(UpdateCommunitySpeciesOutcome.Updated, await ToDtoAsync(snapshot, ct));
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

    public async Task<bool> DeleteAsync(int specCode, CancellationToken ct = default)
    {
        var snapshot = await repo.GetCommunityByCodeAsync(specCode, ct);
        if (snapshot is null || snapshot.ContributedBy != currentUser.UserId) return false;

        // Đã verified = đang public (search/detail đã đọc snapshot này) — không cho tự xoá, chỉ admin mới xử lý được.
        if (snapshot.IsVerified) return false;

        // Cả folder: ảnh bìa hiện tại + bản upload chưa confirm (nếu có).
        await storage.DeleteByPrefixAsync(FishDexObjectKeys.CommunityFolder(specCode), ct);

        repo.Remove(snapshot);
        await repo.SaveChangesAsync(ct);
        logger.LogInformation("Community species {SpecCode} deleted by {UserId}", specCode, currentUser.UserId);
        return true;
    }

    public async Task<CommunityImageUploadResultDto?> RequestImageUploadAsync(
        int specCode, string fileName, string contentType, CancellationToken ct = default)
    {
        if (!ImageExtensions.TryGetValue(contentType, out var ext)) return null;

        var snapshot = await repo.GetCommunityByCodeAsync(specCode, ct);
        if (snapshot is null || snapshot.ContributedBy != currentUser.UserId) return null;

        var objectKey = FishDexObjectKeys.CommunityCover(specCode, ext);

        var uploadUrl = await storage.GeneratePresignedPutUrlAsync(objectKey, contentType, ct);
        if (uploadUrl is null) return null;

        // Dọn các bản upload chưa confirm ở định dạng khác — mỗi loài chỉ giữ tối đa ảnh bìa hiện tại + 1 bản đang upload.
        // KHÔNG đụng ảnh bìa hiện tại: nó chỉ được thay ở bước confirm, khi ảnh mới đã qua kiểm tra dung lượng.
        foreach (var otherExt in ImageExtensions.Values.Where(e => e != ext))
        {
            var staleKey = FishDexObjectKeys.CommunityCover(specCode, otherExt);
            if (staleKey != snapshot.ThumbnailObjectKey)
            {
                await storage.DeleteAsync(staleKey, ct);
            }
        }

        return new CommunityImageUploadResultDto(uploadUrl, objectKey);
    }

    public async Task<ConfirmCommunityImageOutcome> ConfirmImageUploadAsync(int specCode, string objectKey, CancellationToken ct = default)
    {
        var snapshot = await repo.GetCommunityByCodeAsync(specCode, ct);
        if (snapshot is null || snapshot.ContributedBy != currentUser.UserId || !IsCoverKeyOf(specCode, objectKey))
        {
            return ConfirmCommunityImageOutcome.NotFound;
        }

        // Dung lượng THẬT trên storage — presigned PUT không chặn được, client gửi bao nhiêu R2 nhận bấy nhiêu.
        var size = await storage.GetObjectSizeAsync(objectKey, ct);
        if (size is null)
        {
            return ConfirmCommunityImageOutcome.NotUploaded;
        }

        if (size > FishDexObjectKeys.CommunityCoverMaxBytes)
        {
            await storage.DeleteAsync(objectKey, ct);
            if (snapshot.ThumbnailObjectKey == objectKey)
            {
                // Cùng định dạng = cùng key: file quá cỡ đã ghi đè ảnh bìa cũ, gỡ luôn để không trỏ vào object đã xoá.
                snapshot.ThumbnailObjectKey = null;
                await repo.SaveChangesAsync(ct);
            }

            logger.LogWarning("Community image {Key} rejected: {Bytes} bytes > {Max} (user {UserId})", objectKey, size, FishDexObjectKeys.CommunityCoverMaxBytes, currentUser.UserId);
            return ConfirmCommunityImageOutcome.TooLarge;
        }

        if (!string.IsNullOrEmpty(snapshot.ThumbnailObjectKey) && snapshot.ThumbnailObjectKey != objectKey)
        {
            await storage.DeleteAsync(snapshot.ThumbnailObjectKey, ct);
        }

        snapshot.ThumbnailObjectKey = objectKey;
        await repo.SaveChangesAsync(ct);
        return ConfirmCommunityImageOutcome.Confirmed;
    }

    /// <summary>Key đúng là ảnh bìa của loài này ở một định dạng được phép — client không confirm được key tuỳ ý.</summary>
    private static bool IsCoverKeyOf(int specCode, string objectKey)
    {
        return ImageExtensions.Values.Any(ext => FishDexObjectKeys.CommunityCover(specCode, ext) == objectKey);
    }

    // ─────────────────────────────────────────────────────────────────────────

    private SpeciesSnapshot BuildSnapshot(int specCode, SubmitCommunitySpeciesRequest r)
    {
        var snapshot = new SpeciesSnapshot
        {
            SpecCode      = specCode,
            DataSource    = SnapshotDataSource.Community,
            IsVerified    = false,
            ContributedBy = currentUser.UserId,
            PopulatedFrom = SnapshotPopulatedFrom.Manual,
            PopulatedAt   = DateTime.UtcNow,
        };
        ApplyRequest(snapshot, r);
        return snapshot;
    }

    /// <summary>Gán các field editable từ request lên snapshot — dùng chung cho submit (tạo mới) và update.</summary>
    private static void ApplyRequest(SpeciesSnapshot snapshot, SubmitCommunitySpeciesRequest r)
    {
        snapshot.SpeciesName      = r.SpeciesName.Trim();
        snapshot.CommonName       = r.CommonName?.Trim();
        snapshot.FamilyName       = r.FamilyName?.Trim();
        snapshot.GenusName        = r.GenusName?.Trim();
        snapshot.SuggestedKind    = r.SuggestedKind;
        snapshot.WaterType        = r.WaterType;
        snapshot.TempMin          = r.TempMin;
        snapshot.TempMax          = r.TempMax;
        snapshot.PhMin            = r.PhMin;
        snapshot.PhMax            = r.PhMax;
        snapshot.DhMin            = r.DhMin;
        snapshot.DhMax            = r.DhMax;
        snapshot.Length           = r.Length;
        snapshot.LongevityCaptive = r.LongevityCaptive;
        snapshot.FeedingType      = r.FeedingType;
        snapshot.FeedingPosition  = r.FeedingPosition;
        snapshot.ActivityPattern  = r.ActivityPattern;
        snapshot.RequiresLiveFood = r.RequiresLiveFood;
        snapshot.Aggressiveness   = r.Aggressiveness;
        snapshot.FinNippingRisk   = r.FinNippingRisk;
        snapshot.JumpingRisk      = r.JumpingRisk;
        snapshot.CareLevel        = r.CareLevel;
        snapshot.MinTankLiters    = r.MinTankLiters;
    }

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
            s.SuggestedKind, s.Kind,
            s.TempMin, s.TempMax, s.PhMin, s.PhMax, s.DhMin, s.DhMax,
            s.Length, s.LongevityCaptive, s.FeedingType, s.FeedingPosition, s.ActivityPattern,
            s.RequiresLiveFood, s.Aggressiveness, s.FinNippingRisk, s.JumpingRisk,
            s.CareLevel, s.MinTankLiters, s.ThumbnailObjectKey);
    }
}
