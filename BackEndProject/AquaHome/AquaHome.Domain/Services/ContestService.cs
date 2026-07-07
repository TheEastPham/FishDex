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

    private static readonly HashSet<string> AllowedContentTypes = ["video/mp4", "video/quicktime", "video/webm"];

    private string Env => storageOptions.Value.Environment;

    public async Task<IReadOnlyList<ContestDto>> GetAllAsync(CancellationToken ct = default)
        => (await contestRepo.GetAllAsync(ct)).Select(ToDto).ToList();

    public async Task<IReadOnlyList<ContestDto>> GetActiveAsync(CancellationToken ct = default)
        => (await contestRepo.GetActiveAsync(ct)).Select(ToDto).ToList();

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
        await contestRepo.SaveChangesAsync(ct);
        return ToDto(contest);
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
        return ToDto(contest);
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
        return entries
            .Where(e => e.Status == (int)ContestEntryStatus.Published)
            .Select(e => new LeaderboardEntryDto(e.Id, e.AquariumSnapshotId, e.YouTubeVideoId, e.YouTubeViewCount, e.Rank))
            .ToList();
    }

    public async Task<IReadOnlyList<ContestEntryDto>> GetPendingReviewAsync(CancellationToken ct = default)
        => (await entryRepo.GetByStatusAsync((int)ContestEntryStatus.UploadedDraft, ct)).Select(ToDto).ToList();

    private static ContestDto ToDto(Contest c) => new(
        c.Id, c.Title, c.Description, c.YouTubePlaylistId, c.StartAt, c.EndAt, (ContestStatus)c.Status);

    private static ContestEntryDto ToDto(ContestEntry e) => new(
        e.Id, e.ContestId, e.AquariumSnapshotId, e.YouTubeVideoId, e.YouTubeViewCount, e.Rank, (ContestEntryStatus)e.Status, e.SubmittedAt);
}
