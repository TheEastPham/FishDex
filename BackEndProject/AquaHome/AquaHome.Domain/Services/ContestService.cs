using AquaHome.Common.Enums;
using AquaHome.Domain.Contests;
using AquaHome.Domain.DTOs;
using AquaHome.Domain.Enums;
using AquaHome.Domain.Exceptions;
using AquaHome.Domain.Extensions;
using AquaHome.Domain.Services.Interfaces;
using AquaHome.Domain.Storage;
using AquaHome.EFCore.Entity;
using AquaHome.EFCore.Repository.Interface;
using FishLover.Shared.Services;
using FishLover.Storage;
using Microsoft.Extensions.Logging;

namespace AquaHome.Domain.Services;

public class ContestService(
    IContestRepository contestRepo,
    IContestEntryRepository entryRepo,
    IContestPrizeTierRepository prizeTierRepo,
    IContestSponsorRepository sponsorRepo,
    IAquariumSnapshotRepository snapshotRepo,
    IStorageService storage,
    UploadVerifier uploadVerifier,
    IYouTubeUploadService youTube,
    ICurrentUserSession currentUser,
    AquaHomeObjectKeys keys,
    ILogger<ContestService> logger) : IContestService
{
    // R2 free tier 10GB — hard block ở 90% (9GB) trước khi issue presigned PUT URL
    private const long HardBlockBytes = 9L * 1024 * 1024 * 1024;
    private const long MaxVideoUploadBytes = 500L * 1024 * 1024; // 500MB — đủ cho video 2-5 phút 1080p
    private const int MaxTitleLength = 100;       // trùng giới hạn title của YouTube
    private const int MaxDescriptionLength = 100; // chốt với user — mô tả ngắn gọn
    private const long MaxLogoUploadBytes = 300L * 1024;         // 300KB — logo sponsor và ảnh hạng giải (FE nén về ~200KB)
    private static readonly UploadPolicy LogoPolicy = new(MaxLogoUploadBytes);

    // Mỗi bể vẫn chỉ nộp 1 lần/contest; trần này chặn 1 account nhiều bể chiếm hết lượt upload YouTube.
    private const int MaxEntriesPerUserPerContest = 3;

    // Đuôi file suy từ content-type (đã ký vào presigned PUT), không lấy theo tên file client gửi.
    private static readonly IReadOnlyDictionary<string, string> VideoExtensions = UploadContentTypes.Videos;

    // Bài mới submit chưa confirm vẫn được tính vào trần staging trong khoảng này (đang upload dở). Quá thì không
    // tính nữa — khai 500MB rồi không upload gì không giữ chỗ được mãi.
    private static readonly TimeSpan UnverifiedUploadWindow = TimeSpan.FromMinutes(30);


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
        if (request.Status.HasValue)
        {
            // Kết thúc bằng form sửa cũng theo luật như trao giải: không để bài nào treo lại.
            if (request.Status == ContestStatus.Ended && contest.Status != (int)ContestStatus.Ended)
                ContestEntryLifecycle.EnsureContestCanEnd(await entryRepo.GetByContestAsync(id, ct));
            contest.Status = (int)request.Status.Value;
        }

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

        if (!VideoExtensions.TryGetValue(request.ContentType, out var ext))
            throw new ContestValidationException("Định dạng video không được hỗ trợ.");

        if (request.FileSizeBytes <= 0 || request.FileSizeBytes > MaxVideoUploadBytes)
            throw new ContestValidationException($"Video vượt quá dung lượng tối đa {MaxVideoUploadBytes / 1024 / 1024}MB.");

        var contest = await contestRepo.GetByIdAsync(contestId, ct)
            ?? throw new ContestValidationException("Contest không tồn tại.");

        var now = DateTime.UtcNow;
        if (contest.Status != (int)ContestStatus.Active || now < contest.StartAt || now > contest.EndAt)
            throw new ContestValidationException("Cuộc thi chưa mở hoặc đã hết hạn nhận bài.");

        var snapshot = await snapshotRepo.GetByIdAsync(request.AquariumSnapshotId, ct);
        if (snapshot is null || snapshot.UserId != currentUser.UserId)
            throw new ContestValidationException("Snapshot không hợp lệ hoặc không thuộc về bạn.");

        var title = request.Title?.Trim();
        if (title?.Length > MaxTitleLength)
            throw new ContestValidationException($"Tên video tối đa {MaxTitleLength} ký tự.");

        var description = request.Description?.Trim();
        if (description?.Length > MaxDescriptionLength)
            throw new ContestValidationException($"Mô tả tối đa {MaxDescriptionLength} ký tự.");

        // Nộp trùng cùng 1 bể cho cùng contest = upload thêm 1 video YouTube trùng nội dung,
        // tốn 1.600/10.000 quota units mỗi ngày. Bài bị từ chối hoặc không đăng được thì vẫn cho nộp lại.
        // Hết lượt YouTube hôm nay không chặn ở đây: bài nằm hàng chờ, Worker đăng khi có lượt (ADR 0005).
        var mine = (await entryRepo.GetByUserAndContestAsync(currentUser.UserId, contestId, ct))
            .Where(ContestEntryLifecycle.IsActive)
            .ToList();
        if (mine.Any(e => e.AquariumSnapshotId == request.AquariumSnapshotId))
            throw new ContestValidationException("Bạn đã dự thi bằng bể này rồi. Hãy chọn bể khác.");

        if (mine.Count >= MaxEntriesPerUserPerContest)
            throw new ContestValidationException(
                $"Mỗi người nộp tối đa {MaxEntriesPerUserPerContest} bài cho một cuộc thi (bài bị từ chối hoặc không đăng được không tính).");

        // Hard block 90% (9GB) — check trước khi issue presigned PUT URL
        var stagingTotal = await entryRepo.SumStagingVideoBytesAsync(DateTime.UtcNow - UnverifiedUploadWindow, ct);
        if (stagingTotal + request.FileSizeBytes > HardBlockBytes)
        {
            logger.LogWarning("R2 staging hard block hit: {Total} + {New} > {Limit}", stagingTotal, request.FileSizeBytes, HardBlockBytes);
            throw new StorageOverloadedException("Hệ thống đang quá tải, vui lòng thử lại sau");
        }

        var entryId = Guid.NewGuid();
        var objectKey = keys.ContestEntryVideo(contestId, entryId, ext);

        var uploadUrl = await storage.GeneratePresignedPutUrlAsync(objectKey, request.ContentType, ct)
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
            Title = string.IsNullOrWhiteSpace(title) ? null : title,
            Description = string.IsNullOrWhiteSpace(description) ? null : description,
            Status = (int)ContestEntryStatus.Pending,
            SubmittedAt = DateTime.UtcNow,
        };

        await entryRepo.AddAsync(entry, ct);
        await entryRepo.SaveChangesAsync(ct);

        return new SubmitEntryResultDto(entry.Id, uploadUrl, objectKey);
    }

    /// <summary>
    /// Kiểm file thật trên R2 rồi đưa bài vào hàng chờ đăng YouTube; Worker đăng sau (ADR 0005). Nhanh — chỉ HEAD 1
    /// object, không còn kéo video lên YouTube trong request. False nếu bài không thuộc user, không chờ xác nhận,
    /// request khác đang xác nhận, hoặc file không đạt (bài chuyển Failed kèm lý do).
    /// </summary>
    public async Task<bool> ConfirmUploadAsync(Guid contestId, Guid entryId, CancellationToken ct = default)
    {
        var entry = await entryRepo.GetByIdAsync(entryId, ct);
        if (entry is null || entry.ContestId != contestId || entry.UserId != currentUser.UserId) return false;
        if (!ContestEntryLifecycle.CanConfirm(entry)) return false;

        // Không xét EndAt: bài nộp sát giờ đóng vẫn confirm được sau khi tải xong video; admin chuyển
        // contest sang Ended mới là chặn hẳn.
        var contest = await contestRepo.GetByIdAsync(contestId, ct);
        if (contest?.Status != (int)ContestStatus.Active)
            throw new ContestValidationException("Cuộc thi đã đóng, không nhận thêm bài.");

        // N request confirm song song cho cùng 1 bài: chỉ 1 request chiếm được.
        if (!await entryRepo.TryClaimForCheckAsync(entry.Id, ct)) return false;
        ContestEntryLifecycle.StartCheck(entry);
        await entryRepo.SaveChangesAsync(ct); // đồng bộ change tracker với DB, để các bước sau ghi được

        // Presigned PUT không chặn được dung lượng → tin file thật trên R2 chứ không tin số client khai.
        // Dung lượng khai thiếu (null) bị coi là lệch: -1 không bao giờ bằng dung lượng thật.
        var policy = new UploadPolicy(MaxVideoUploadBytes, entry.VideoSizeBytes ?? -1);
        UploadVerdict verdict;
        try
        {
            verdict = await uploadVerifier.VerifyAsync(entry.VideoR2Key!, policy, ct);
        }
        catch
        {
            ContestEntryLifecycle.CheckUnavailable(entry);
            await entryRepo.SaveChangesAsync(CancellationToken.None); // client ngắt cũng phải trả bài về Pending
            throw;
        }

        ContestEntryLifecycle.FileChecked(entry, verdict, DateTime.UtcNow);
        await entryRepo.SaveChangesAsync(CancellationToken.None);

        if (!verdict.IsOk)
            logger.LogWarning("Contest entry {EntryId} failed upload check: {Verdict}, R2 size {Actual} vs declared {Declared}",
                entry.Id, verdict.Kind, verdict.Size, entry.VideoSizeBytes);
        return verdict.IsOk;
    }

    public async Task<bool> ApproveEntryAsync(Guid contestId, Guid entryId, CancellationToken ct = default)
    {
        var entry = await entryRepo.GetByIdAsync(entryId, ct);
        if (entry is null || entry.ContestId != contestId) return false;

        var contest = await contestRepo.GetByIdAsync(contestId, ct);
        ContestEntryLifecycle.EnsureCanApprove(entry, contest);
        try
        {
            await youTube.SetPublicAsync(entry.YouTubeVideoId!, contest?.YouTubePlaylistId, ct);
        }
        catch (Exception ex)
        {
            // Không đổi Status: video vẫn Unlisted, admin sửa playlist ID rồi approve lại được.
            logger.LogError(ex, "Approve entry {EntryId} thất bại khi set public/add playlist", entryId);
            throw new ContestValidationException(
                "Không thể công khai video lên YouTube. Kiểm tra lại Playlist ID của cuộc thi rồi thử lại.");
        }

        ContestEntryLifecycle.Publish(entry);
        await entryRepo.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> RejectEntryAsync(Guid contestId, Guid entryId, string reason, CancellationToken ct = default)
    {
        var entry = await entryRepo.GetByIdAsync(entryId, ct);
        if (entry is null || entry.ContestId != contestId) return false;

        var contest = await contestRepo.GetByIdAsync(contestId, ct);
        ContestEntryLifecycle.EnsureCanReject(entry, contest);

        // Xoá video trước: YouTube lỗi thì bài giữ nguyên trạng thái, admin từ chối lại được.
        if (!string.IsNullOrEmpty(entry.YouTubeVideoId))
            await youTube.DeleteVideoAsync(entry.YouTubeVideoId, ct);

        ContestEntryLifecycle.Reject(entry, reason);
        await entryRepo.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<ContestEntryDto>> GetMyEntriesAsync(Guid contestId, CancellationToken ct = default)
    {
        var entries = await entryRepo.GetByUserAndContestAsync(currentUser.UserId, contestId, ct);
        var tiers = (await prizeTierRepo.GetByContestAsync(contestId, ct)).ToDictionary(t => t.Id);

        var result = new List<ContestEntryDto>(entries.Count);
        foreach (var e in entries)
        {
            var tierName = e.PrizeTierId.HasValue && tiers.TryGetValue(e.PrizeTierId.Value, out var t) ? t.Name : null;
            result.Add(await ToDtoWithSnapshotAsync(e, tierName, ct));
        }
        return result;
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
    {
        // pending review chưa từng có tier — finalize chỉ chạy sau khi Published
        var entries = await entryRepo.GetByStatusAsync(ContestEntryStatus.UploadedDraft, ct);

        var result = new List<ContestEntryDto>(entries.Count);
        foreach (var e in entries)
            result.Add(await ToDtoWithSnapshotAsync(e, prizeTierName: null, ct));
        return result;
    }

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
        return await ToDtoAsync(tier, ct);
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
        return await ToDtoAsync(tier, ct);
    }

    public async Task<bool> DeletePrizeTierAsync(Guid contestId, Guid tierId, CancellationToken ct = default)
    {
        var tier = await prizeTierRepo.GetByIdAsync(tierId, ct);
        if (tier is null || tier.ContestId != contestId) return false;

        if (!string.IsNullOrEmpty(tier.ImageObjectKey))
            await storage.DeleteAsync(tier.ImageObjectKey, ct);

        prizeTierRepo.Remove(tier);
        await prizeTierRepo.SaveChangesAsync(ct);
        return true;
    }

    public async Task<PrizeTierImageUploadResultDto?> RequestPrizeTierImageUploadAsync(
        Guid contestId, Guid tierId, string fileName, string contentType, CancellationToken ct = default)
    {
        // Đuôi suy từ content-type (đã ký vào presigned PUT), không lấy theo tên file client gửi.
        if (!UploadContentTypes.Logos.TryGetValue(contentType, out var ext)) return null;

        var tier = await prizeTierRepo.GetByIdAsync(tierId, ct);
        if (tier is null || tier.ContestId != contestId) return null;

        var objectKey = keys.ContestPrizeTierLogo(contestId, tierId, ext);

        var uploadUrl = await storage.GeneratePresignedPutUrlAsync(objectKey, contentType, ct);
        if (uploadUrl is null) return null;

        tier.ImageObjectKey = objectKey;
        await prizeTierRepo.SaveChangesAsync(ct);

        return new PrizeTierImageUploadResultDto(uploadUrl, objectKey);
    }

    /// <summary>
    /// Xác minh ảnh hạng giải đã upload: file thật phải có và không quá <see cref="MaxLogoUploadBytes"/>. Không thì gỡ key
    /// khỏi hạng giải (verifier đã xoá object quá cỡ) và ném <see cref="ContestValidationException"/>.
    /// False nếu hạng giải không tồn tại hoặc chưa xin URL upload.
    /// </summary>
    public async Task<bool> ConfirmPrizeTierImageUploadAsync(Guid contestId, Guid tierId, CancellationToken ct = default)
    {
        var tier = await prizeTierRepo.GetByIdAsync(tierId, ct);
        if (tier is null || tier.ContestId != contestId || string.IsNullOrEmpty(tier.ImageObjectKey)) return false;

        var verdict = await uploadVerifier.VerifyAsync(tier.ImageObjectKey, LogoPolicy, ct);
        if (verdict.IsOk) return true;

        logger.LogWarning("Prize tier image {Key} rejected: {Verdict}, R2 size {Size}", tier.ImageObjectKey, verdict.Kind, verdict.Size);
        tier.ImageObjectKey = null;
        await prizeTierRepo.SaveChangesAsync(ct);
        throw LogoRejected(verdict.Kind);
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
            Address = request.Address,
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
        if (request.Address is not null) sponsor.Address = request.Address;
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
        // Đuôi suy từ content-type (đã ký vào presigned PUT), không lấy theo tên file client gửi.
        if (!UploadContentTypes.Logos.TryGetValue(contentType, out var ext)) return null;

        var sponsor = await sponsorRepo.GetByIdAsync(sponsorId, ct);
        if (sponsor is null || sponsor.ContestId != contestId) return null;

        var objectKey = keys.ContestSponsorLogo(contestId, sponsorId, ext);

        var uploadUrl = await storage.GeneratePresignedPutUrlAsync(objectKey, contentType, ct);
        if (uploadUrl is null) return null;

        sponsor.LogoObjectKey = objectKey;
        await sponsorRepo.SaveChangesAsync(ct);

        return new SponsorLogoUploadResultDto(uploadUrl, objectKey);
    }

    /// <summary>
    /// Xác minh logo sponsor đã upload — cùng quy tắc với ảnh hạng giải. False nếu sponsor không tồn tại hoặc chưa xin URL.
    /// </summary>
    public async Task<bool> ConfirmSponsorLogoUploadAsync(Guid contestId, Guid sponsorId, CancellationToken ct = default)
    {
        var sponsor = await sponsorRepo.GetByIdAsync(sponsorId, ct);
        if (sponsor is null || sponsor.ContestId != contestId || string.IsNullOrEmpty(sponsor.LogoObjectKey)) return false;

        var verdict = await uploadVerifier.VerifyAsync(sponsor.LogoObjectKey, LogoPolicy, ct);
        if (verdict.IsOk) return true;

        logger.LogWarning("Sponsor logo {Key} rejected: {Verdict}, R2 size {Size}", sponsor.LogoObjectKey, verdict.Kind, verdict.Size);
        sponsor.LogoObjectKey = null;
        await sponsorRepo.SaveChangesAsync(ct);
        throw LogoRejected(verdict.Kind);
    }

    private static ContestValidationException LogoRejected(UploadVerdictKind kind)
    {
        return new ContestValidationException(kind == UploadVerdictKind.NotUploaded
            ? "Không tìm thấy ảnh đã tải lên. Bạn hãy tải lại."
            : $"Ảnh vượt quá dung lượng tối đa {MaxLogoUploadBytes / 1024}KB.");
    }

    // ── Finalize ─────────────────────────────────────────────────
    public async Task<bool> FinalizeAsync(Guid contestId, FinalizeContestRequest request, CancellationToken ct = default)
    {
        var contest = await contestRepo.GetByIdAsync(contestId, ct)
            ?? throw new ContestValidationException("Contest không tồn tại.");

        var tiers = (await prizeTierRepo.GetByContestAsync(contestId, ct)).ToDictionary(t => t.Id);
        var contestEntries = await entryRepo.GetByContestAsync(contestId, ct);
        ContestEntryLifecycle.EnsureContestCanEnd(contestEntries);
        var entries = contestEntries.ToDictionary(e => e.Id);

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

        var tierDtos = new List<ContestPrizeTierDto>(tiers.Count);
        foreach (var t in tiers) tierDtos.Add(await ToDtoAsync(t, ct));

        return new ContestDto(
            c.Id, c.Title, c.Description, c.YouTubePlaylistId, c.StartAt, c.EndAt, (ContestStatus)c.Status,
            tierDtos, sponsorDtos);
    }

    private async Task<ContestPrizeTierDto> ToDtoAsync(ContestPrizeTier t, CancellationToken ct)
    {
        var imageUrl = string.IsNullOrEmpty(t.ImageObjectKey) ? null : await storage.GetPresignedUrlAsync(t.ImageObjectKey, ct);
        return new ContestPrizeTierDto(t.Id, t.Name, (PrizeTierLevel)t.TierLevel, t.SlotCount, t.DisplayOrder, t.Description, imageUrl);
    }

    private async Task<ContestSponsorDto> ToDtoAsync(ContestSponsor s, CancellationToken ct)
    {
        var logoUrl = string.IsNullOrEmpty(s.LogoObjectKey) ? null : await storage.GetPresignedUrlAsync(s.LogoObjectKey, ct);
        return new ContestSponsorDto(s.Id, s.Name, s.WebsiteUrl, s.Address, logoUrl, (SponsorTier)s.SponsorTier, s.DisplayOrder);
    }

    private static ContestEntryDto ToDto(
        ContestEntry e, string? prizeTierName,
        string? aquariumName = null, string? ownerNickname = null, string? snapshotSlug = null) => new(
        e.Id, e.ContestId, e.AquariumSnapshotId, e.YouTubeVideoId, e.YouTubeViewCount,
        e.PrizeTierId, prizeTierName, (ContestEntryStatus)e.Status, e.SubmittedAt,
        e.Title, e.Description, e.RejectionReason,
        aquariumName, ownerNickname, snapshotSlug);

    /// <summary>
    /// Kèm thông tin bể từ snapshot — admin duyệt và người dự thi cần biết bài này là bể nào, của ai,
    /// thay vì chỉ thấy mỗi ngày nộp. Slug để FE link sang trang public của bể (đã có sẵn ảnh cover).
    /// </summary>
    private async Task<ContestEntryDto> ToDtoWithSnapshotAsync(ContestEntry e, string? prizeTierName, CancellationToken ct)
    {
        var snapshot = await snapshotRepo.GetByIdAsync(e.AquariumSnapshotId, ct);
        var data = SnapshotDataReader.TryRead(snapshot?.SnapshotData);
        return ToDto(e, prizeTierName, data?.AquariumName, data?.OwnerNickname, snapshot?.Slug);
    }
}
