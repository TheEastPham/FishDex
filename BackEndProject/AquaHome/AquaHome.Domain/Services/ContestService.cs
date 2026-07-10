using AquaHome.Domain.DTOs;
using AquaHome.Domain.Enums;
using AquaHome.Domain.Exceptions;
using AquaHome.Domain.Services.Interfaces;
using AquaHome.Domain.Settings;
using AquaHome.EFCore.Entity;
using AquaHome.EFCore.Repository.Interface;
using FishLover.Shared.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AquaHome.Domain.Services;

public class ContestService(
    IContestRepository contestRepo,
    IContestEntryRepository entryRepo,
    IContestPrizeTierRepository prizeTierRepo,
    IContestSponsorRepository sponsorRepo,
    IAquariumSnapshotRepository snapshotRepo,
    IStorageService storage,
    IYouTubeUploadService youTube,
    ICurrentUserSession currentUser,
    IOptions<StorageSettings> storageOptions,
    ILogger<ContestService> logger) : IContestService
{
    // R2 free tier 10GB — hard block ở 90% (9GB) trước khi issue presigned PUT URL
    private const long HardBlockBytes = 9L * 1024 * 1024 * 1024;
    private const long MaxVideoUploadBytes = 500L * 1024 * 1024; // 500MB — đủ cho video 2-5 phút 1080p
    private const long MaxLogoUploadBytes = 2L * 1024 * 1024;    // 2MB — logo sponsor

    private static readonly HashSet<string> AllowedContentTypes = ["video/mp4", "video/quicktime", "video/webm"];
    private static readonly HashSet<string> AllowedLogoContentTypes = ["image/jpeg", "image/png", "image/webp", "image/svg+xml"];

    private string Env => storageOptions.Value.Environment;

    public async Task<IReadOnlyList<ContestDto>> GetAllAsync(CancellationToken ct = default)
    {
        var contests = await contestRepo.GetAllAsync(ct);
        var result = new List<ContestDto>(contests.Count);
        foreach (var c in contests) result.Add(await ToDtoAsync(c, ct));
        return result;
    }

    public async Task<IReadOnlyList<ContestDto>> GetActiveAsync(CancellationToken ct = default)
    {
        var contests = await contestRepo.GetActiveAsync(ct);
        var result = new List<ContestDto>(contests.Count);
        foreach (var c in contests) result.Add(await ToDtoAsync(c, ct));
        return result;
    }

    public async Task<ContestDto> CreateAsync(CreateContestRequest request, CancellationToken ct = default)
    {
        var contest = new Contest
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            YouTubePlaylistId = request.YouTubePlaylistId,
            StartAt = request.StartAt,
            EndAt = request.EndAt,
            Status = (int)ContestStatus.Draft,
            CreatedBy = currentUser.UserId,
        };

        await contestRepo.AddAsync(contest, ct);

        // Seed 4 tier mẫu — admin sửa tên/SlotCount hoặc thêm/xóa tier tùy contest
        var presetTiers = new[]
        {
            new ContestPrizeTier { Id = Guid.NewGuid(), ContestId = contest.Id, Name = "Giải Nhất", TierLevel = (int)PrizeTierLevel.Gold, SlotCount = 1, DisplayOrder = 1 },
            new ContestPrizeTier { Id = Guid.NewGuid(), ContestId = contest.Id, Name = "Giải Nhì", TierLevel = (int)PrizeTierLevel.Silver, SlotCount = 1, DisplayOrder = 2 },
            new ContestPrizeTier { Id = Guid.NewGuid(), ContestId = contest.Id, Name = "Giải Ba", TierLevel = (int)PrizeTierLevel.Bronze, SlotCount = 1, DisplayOrder = 3 },
            new ContestPrizeTier { Id = Guid.NewGuid(), ContestId = contest.Id, Name = "Giải Khuyến khích", TierLevel = (int)PrizeTierLevel.Encouragement, SlotCount = 0, DisplayOrder = 4 },
        };
        foreach (var tier in presetTiers) await prizeTierRepo.AddAsync(tier, ct);

        await contestRepo.SaveChangesAsync(ct);
        return await ToDtoAsync(contest, ct);
    }

    public async Task<ContestDto?> UpdateAsync(Guid id, UpdateContestRequest request, CancellationToken ct = default)
    {
        var contest = await contestRepo.GetByIdAsync(id, ct);
        if (contest is null) return null;

        if (request.Title is not null) contest.Title = request.Title;
        if (request.Description is not null) contest.Description = request.Description;
        if (request.YouTubePlaylistId is not null) contest.YouTubePlaylistId = request.YouTubePlaylistId;
        if (request.StartAt.HasValue) contest.StartAt = request.StartAt.Value;
        if (request.EndAt.HasValue) contest.EndAt = request.EndAt.Value;
        if (request.Status.HasValue) contest.Status = (int)request.Status.Value;

        await contestRepo.SaveChangesAsync(ct);
        return await ToDtoAsync(contest, ct);
    }

    public async Task<SubmitEntryResultDto> SubmitEntryAsync(Guid contestId, SubmitEntryRequest request, CancellationToken ct = default)
    {
        // Video 2-5 phút (120-300s)
        if (request.VideoDurationSeconds < 120 || request.VideoDurationSeconds > 300)
            throw new ContestValidationException(
                request.VideoDurationSeconds < 120
                    ? "Video quá ngắn, tối thiểu 2 phút."
                    : "Video quá dài, tối đa 5 phút.");

        if (!AllowedContentTypes.Contains(request.ContentType))
            throw new ContestValidationException("Định dạng video không được hỗ trợ.");

        if (request.FileSizeBytes > MaxVideoUploadBytes)
            throw new ContestValidationException($"Video vượt quá dung lượng tối đa {MaxVideoUploadBytes / 1024 / 1024}MB.");

        var contest = await contestRepo.GetByIdAsync(contestId, ct)
            ?? throw new ContestValidationException("Contest không tồn tại.");

        var snapshot = await snapshotRepo.GetByIdAsync(request.AquariumSnapshotId, ct);
        if (snapshot is null || snapshot.UserId != currentUser.UserId)
            throw new ContestValidationException("Snapshot không hợp lệ hoặc không thuộc về bạn.");

        // Hard block 90% (9GB) — check trước khi issue presigned PUT URL
        var stagingTotal = await entryRepo.SumStagingVideoBytesAsync(ct);
        if (stagingTotal + request.FileSizeBytes > HardBlockBytes)
        {
            logger.LogWarning("R2 staging hard block hit: {Total} + {New} > {Limit}", stagingTotal, request.FileSizeBytes, HardBlockBytes);
            throw new StorageOverloadedException("Hệ thống đang quá tải, vui lòng thử lại sau");
        }

        var entryId = Guid.NewGuid();
        var ext = Path.GetExtension(request.FileName);
        var objectKey = $"aquahome/{Env}/contests/{contestId}/{entryId}{ext}";

        var uploadUrl = await storage.GeneratePresignedPutUrlAsync(objectKey, request.ContentType, MaxVideoUploadBytes, ct)
            ?? throw new ContestValidationException("Không thể tạo upload URL, vui lòng thử lại.");

        var entry = new ContestEntry
        {
            Id = entryId,
            ContestId = contestId,
            AquariumSnapshotId = request.AquariumSnapshotId,
            UserId = currentUser.UserId,
            VideoR2Key = objectKey,
            VideoSizeBytes = request.FileSizeBytes,
            VideoDurationSeconds = request.VideoDurationSeconds,
            Status = (int)ContestEntryStatus.Pending,
            SubmittedAt = DateTime.UtcNow,
        };

        await entryRepo.AddAsync(entry, ct);
        await entryRepo.SaveChangesAsync(ct);

        return new SubmitEntryResultDto(entry.Id, uploadUrl, objectKey);
    }

    public async Task<bool> ConfirmUploadAsync(Guid contestId, Guid entryId, CancellationToken ct = default)
    {
        var entry = await entryRepo.GetByIdAsync(entryId, ct);
        if (entry is null || entry.ContestId != contestId || entry.UserId != currentUser.UserId) return false;
        if (entry.Status != (int)ContestEntryStatus.Pending || string.IsNullOrEmpty(entry.VideoR2Key)) return false;

        entry.Status = (int)ContestEntryStatus.Validating;
        await entryRepo.SaveChangesAsync(ct);

        var contest = await contestRepo.GetByIdAsync(contestId, ct);
        var videoId = await youTube.UploadUnlistedAsync(
            entry.VideoR2Key, contest?.Title ?? "FishLover Contest Entry", "Submitted via FishLover Public Aquarium Contest", ct);

        if (videoId is null)
        {
            entry.Status = (int)ContestEntryStatus.Rejected;
            await entryRepo.SaveChangesAsync(ct);
            return false;
        }

        entry.YouTubeVideoId = videoId;
        entry.Status = (int)ContestEntryStatus.UploadedDraft;
        await storage.DeleteAsync(entry.VideoR2Key, ct); // R2 chỉ là staging
        entry.VideoR2Key = null;
        await entryRepo.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ApproveEntryAsync(Guid contestId, Guid entryId, CancellationToken ct = default)
    {
        var entry = await entryRepo.GetByIdAsync(entryId, ct);
        if (entry is null || entry.ContestId != contestId) return false;
        if (entry.Status != (int)ContestEntryStatus.UploadedDraft || entry.YouTubeVideoId is null) return false;

        var contest = await contestRepo.GetByIdAsync(contestId, ct);
        await youTube.SetPublicAsync(entry.YouTubeVideoId, contest?.YouTubePlaylistId, ct);

        entry.Status = (int)ContestEntryStatus.Published;
        await entryRepo.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> RejectEntryAsync(Guid contestId, Guid entryId, CancellationToken ct = default)
    {
        var entry = await entryRepo.GetByIdAsync(entryId, ct);
        if (entry is null || entry.ContestId != contestId) return false;

        if (!string.IsNullOrEmpty(entry.YouTubeVideoId))
            await youTube.DeleteVideoAsync(entry.YouTubeVideoId, ct);

        if (!string.IsNullOrEmpty(entry.VideoR2Key))
            await storage.DeleteAsync(entry.VideoR2Key, ct);

        entry.Status = (int)ContestEntryStatus.Rejected;
        entry.VideoR2Key = null;
        await entryRepo.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<LeaderboardEntryDto>> GetLeaderboardAsync(Guid contestId, CancellationToken ct = default)
    {
        var entries = await entryRepo.GetByContestAsync(contestId, ct);
        var tiers = (await prizeTierRepo.GetByContestAsync(contestId, ct)).ToDictionary(t => t.Id);

        return entries
            .Where(e => e.Status == (int)ContestEntryStatus.Published)
            .Select(e =>
            {
                var tier = e.PrizeTierId.HasValue && tiers.TryGetValue(e.PrizeTierId.Value, out var t) ? t : null;
                return new LeaderboardEntryDto(
                    e.Id, e.AquariumSnapshotId, e.YouTubeVideoId, e.YouTubeViewCount,
                    e.PrizeTierId, tier?.Name, tier is null ? null : (PrizeTierLevel)tier.TierLevel);
            })
            .OrderByDescending(x => x.YouTubeViewCount)
            .ToList();
    }

    public async Task<IReadOnlyList<ContestEntryDto>> GetPendingReviewAsync(CancellationToken ct = default)
        => (await entryRepo.GetByStatusAsync((int)ContestEntryStatus.UploadedDraft, ct))
            .Select(e => ToDto(e, prizeTierName: null)) // pending review chưa từng có tier — finalize chỉ chạy sau khi Published
            .ToList();

    // ── Prize tiers ────────────────────────────────────────────
    public async Task<ContestPrizeTierDto> CreatePrizeTierAsync(Guid contestId, CreatePrizeTierRequest request, CancellationToken ct = default)
    {
        var existing = await prizeTierRepo.GetByContestAsync(contestId, ct);
        var tier = new ContestPrizeTier
        {
            Id = Guid.NewGuid(),
            ContestId = contestId,
            Name = request.Name,
            TierLevel = (int)request.TierLevel,
            SlotCount = request.SlotCount,
            DisplayOrder = existing.Count == 0 ? 1 : existing.Max(t => t.DisplayOrder) + 1,
            Description = request.Description,
        };

        await prizeTierRepo.AddAsync(tier, ct);
        await prizeTierRepo.SaveChangesAsync(ct);
        return ToDto(tier);
    }

    public async Task<ContestPrizeTierDto?> UpdatePrizeTierAsync(Guid contestId, Guid tierId, UpdatePrizeTierRequest request, CancellationToken ct = default)
    {
        var tier = await prizeTierRepo.GetByIdAsync(tierId, ct);
        if (tier is null || tier.ContestId != contestId) return null;

        if (request.Name is not null) tier.Name = request.Name;
        if (request.TierLevel.HasValue) tier.TierLevel = (int)request.TierLevel.Value;
        if (request.SlotCount.HasValue) tier.SlotCount = request.SlotCount.Value;
        if (request.DisplayOrder.HasValue) tier.DisplayOrder = request.DisplayOrder.Value;
        if (request.Description is not null) tier.Description = request.Description;

        await prizeTierRepo.SaveChangesAsync(ct);
        return ToDto(tier);
    }

    public async Task<bool> DeletePrizeTierAsync(Guid contestId, Guid tierId, CancellationToken ct = default)
    {
        var tier = await prizeTierRepo.GetByIdAsync(tierId, ct);
        if (tier is null || tier.ContestId != contestId) return false;

        prizeTierRepo.Remove(tier);
        await prizeTierRepo.SaveChangesAsync(ct);
        return true;
    }

    // ── Sponsors ─────────────────────────────────────────────────
    public async Task<ContestSponsorDto> CreateSponsorAsync(Guid contestId, CreateSponsorRequest request, CancellationToken ct = default)
    {
        var existing = await sponsorRepo.GetByContestAsync(contestId, activeOnly: false, ct);
        var sponsor = new ContestSponsor
        {
            Id = Guid.NewGuid(),
            ContestId = contestId,
            Name = request.Name,
            WebsiteUrl = request.WebsiteUrl,
            SponsorTier = (int)request.SponsorTier,
            DisplayOrder = existing.Count == 0 ? 1 : existing.Max(s => s.DisplayOrder) + 1,
            IsActive = true,
        };

        await sponsorRepo.AddAsync(sponsor, ct);
        await sponsorRepo.SaveChangesAsync(ct);
        return await ToDtoAsync(sponsor, ct);
    }

    public async Task<ContestSponsorDto?> UpdateSponsorAsync(Guid contestId, Guid sponsorId, UpdateSponsorRequest request, CancellationToken ct = default)
    {
        var sponsor = await sponsorRepo.GetByIdAsync(sponsorId, ct);
        if (sponsor is null || sponsor.ContestId != contestId) return null;

        if (request.Name is not null) sponsor.Name = request.Name;
        if (request.WebsiteUrl is not null) sponsor.WebsiteUrl = request.WebsiteUrl;
        if (request.SponsorTier.HasValue) sponsor.SponsorTier = (int)request.SponsorTier.Value;
        if (request.DisplayOrder.HasValue) sponsor.DisplayOrder = request.DisplayOrder.Value;
        if (request.IsActive.HasValue) sponsor.IsActive = request.IsActive.Value;

        await sponsorRepo.SaveChangesAsync(ct);
        return await ToDtoAsync(sponsor, ct);
    }

    public async Task<bool> DeleteSponsorAsync(Guid contestId, Guid sponsorId, CancellationToken ct = default)
    {
        var sponsor = await sponsorRepo.GetByIdAsync(sponsorId, ct);
        if (sponsor is null || sponsor.ContestId != contestId) return false;

        if (!string.IsNullOrEmpty(sponsor.LogoObjectKey))
            await storage.DeleteAsync(sponsor.LogoObjectKey, ct);

        sponsorRepo.Remove(sponsor);
        await sponsorRepo.SaveChangesAsync(ct);
        return true;
    }

    public async Task<SponsorLogoUploadResultDto?> RequestSponsorLogoUploadAsync(
        Guid contestId, Guid sponsorId, string fileName, string contentType, CancellationToken ct = default)
    {
        if (!AllowedLogoContentTypes.Contains(contentType)) return null;

        var sponsor = await sponsorRepo.GetByIdAsync(sponsorId, ct);
        if (sponsor is null || sponsor.ContestId != contestId) return null;

        var ext = Path.GetExtension(fileName);
        var objectKey = $"aquahome/{Env}/contests/{contestId}/sponsors/{sponsorId}{ext}";

        var uploadUrl = await storage.GeneratePresignedPutUrlAsync(objectKey, contentType, MaxLogoUploadBytes, ct);
        if (uploadUrl is null) return null;

        sponsor.LogoObjectKey = objectKey;
        await sponsorRepo.SaveChangesAsync(ct);

        return new SponsorLogoUploadResultDto(uploadUrl, objectKey);
    }

    // ── Finalize ─────────────────────────────────────────────────
    public async Task<bool> FinalizeAsync(Guid contestId, FinalizeContestRequest request, CancellationToken ct = default)
    {
        var contest = await contestRepo.GetByIdAsync(contestId, ct)
            ?? throw new ContestValidationException("Contest không tồn tại.");

        var tiers = (await prizeTierRepo.GetByContestAsync(contestId, ct)).ToDictionary(t => t.Id);
        var entries = (await entryRepo.GetByContestAsync(contestId, ct)).ToDictionary(e => e.Id);

        // Validate: không vượt SlotCount mỗi tier
        var countPerTier = request.Assignments
            .Where(a => a.PrizeTierId.HasValue)
            .GroupBy(a => a.PrizeTierId!.Value);

        foreach (var group in countPerTier)
        {
            if (!tiers.TryGetValue(group.Key, out var tier))
                throw new ContestValidationException("Hạng giải không thuộc contest này.");
            if (group.Count() > tier.SlotCount)
                throw new ContestValidationException($"Hạng '{tier.Name}' chỉ có {tier.SlotCount} suất nhưng được gán {group.Count()}.");
        }

        foreach (var assignment in request.Assignments)
        {
            if (!entries.TryGetValue(assignment.EntryId, out var entry) || entry.ContestId != contestId)
                throw new ContestValidationException("Bài dự thi không thuộc contest này.");
            if (entry.Status != (int)ContestEntryStatus.Published)
                throw new ContestValidationException("Chỉ có thể trao giải cho bài dự thi đã Published.");

            entry.PrizeTierId = assignment.PrizeTierId;

            var snapshot = await snapshotRepo.GetByIdAsync(entry.AquariumSnapshotId, ct);
            if (snapshot is null) continue;

            if (assignment.PrizeTierId.HasValue && tiers.TryGetValue(assignment.PrizeTierId.Value, out var tier))
            {
                snapshot.AwardTierName = tier.Name;
                snapshot.AwardTierLevel = tier.TierLevel;
            }
            else
            {
                snapshot.AwardTierName = null;
                snapshot.AwardTierLevel = null;
            }

            snapshot.YoutubeVideoUrl = entry.YouTubeVideoId is not null
                ? $"https://www.youtube.com/watch?v={entry.YouTubeVideoId}"
                : null;
        }

        contest.Status = (int)ContestStatus.Ended;

        // Cùng 1 DbContext (scoped) — SaveChanges qua bất kỳ repo nào cũng commit toàn bộ thay đổi đã track
        await entryRepo.SaveChangesAsync(ct);
        return true;
    }

    private async Task<ContestDto> ToDtoAsync(Contest c, CancellationToken ct)
    {
        var tiers = await prizeTierRepo.GetByContestAsync(c.Id, ct);
        var sponsors = await sponsorRepo.GetByContestAsync(c.Id, activeOnly: false, ct);

        var sponsorDtos = new List<ContestSponsorDto>(sponsors.Count);
        foreach (var s in sponsors) sponsorDtos.Add(await ToDtoAsync(s, ct));

        return new ContestDto(
            c.Id, c.Title, c.Description, c.YouTubePlaylistId, c.StartAt, c.EndAt, (ContestStatus)c.Status,
            tiers.Select(ToDto).ToList(), sponsorDtos);
    }

    private static ContestPrizeTierDto ToDto(ContestPrizeTier t) => new(
        t.Id, t.Name, (PrizeTierLevel)t.TierLevel, t.SlotCount, t.DisplayOrder, t.Description);

    private async Task<ContestSponsorDto> ToDtoAsync(ContestSponsor s, CancellationToken ct)
    {
        var logoUrl = string.IsNullOrEmpty(s.LogoObjectKey) ? null : await storage.GetPresignedUrlAsync(s.LogoObjectKey, ct);
        return new ContestSponsorDto(s.Id, s.Name, s.WebsiteUrl, logoUrl, (SponsorTier)s.SponsorTier, s.DisplayOrder);
    }

    private static ContestEntryDto ToDto(ContestEntry e, string? prizeTierName) => new(
        e.Id, e.ContestId, e.AquariumSnapshotId, e.YouTubeVideoId, e.YouTubeViewCount,
        e.PrizeTierId, prizeTierName, (ContestEntryStatus)e.Status, e.SubmittedAt);
}
