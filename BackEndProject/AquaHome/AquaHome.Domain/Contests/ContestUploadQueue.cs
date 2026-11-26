using AquaHome.Domain.Exceptions;
using AquaHome.Domain.Extensions;
using AquaHome.Domain.Services.Interfaces;
using AquaHome.EFCore.Entity;
using AquaHome.EFCore.Repository.Interface;
using FishLover.Storage;
using Microsoft.Extensions.Logging;

namespace AquaHome.Domain.Contests;

/// <summary>
/// Hàng chờ đăng YouTube (ADR 0005): Worker gọi định kỳ. Đăng lần lượt bài đã xác nhận file, giữ lượt ngay trước
/// mỗi lần đăng; hết lượt chung hoặc YouTube báo hết quota thì dừng, bài còn lại chờ lần quét sau. Chỉ chạy một
/// instance — không khoá bài trước khi đăng.
/// </summary>
public sealed class ContestUploadQueue(
    IContestEntryRepository entryRepo,
    IAquariumSnapshotRepository snapshotRepo,
    IYouTubeUploadService youTube,
    IStorageService storage,
    YouTubeUploadAllowance allowance,
    ILogger<ContestUploadQueue> logger)
{
    /// <summary>Trả số bài đã lên YouTube trong lần quét này.</summary>
    public async Task<int> PostQueuedAsync(CancellationToken ct = default)
    {
        var queue = await entryRepo.GetUploadQueueAsync(ct);
        var posted = 0;

        foreach (var entry in queue)
        {
            if (!ContestEntryLifecycle.IsReadyToPost(entry, entry.Contest)) continue;

            var check = await allowance.CheckAsync(entry.UserId, ct);
            if (check == AllowanceCheck.SystemExhausted) break;
            if (check == AllowanceCheck.UserExhausted) continue;
            if (!await allowance.TryReserveAsync(entry.UserId, ct)) break;

            var (title, description) = await DescribeAsync(entry, entry.Contest, ct);

            string? videoId;
            try
            {
                videoId = await youTube.UploadUnlistedAsync(entry.VideoR2Key!, title, description, ct);
            }
            catch (YouTubeQuotaExceededException)
            {
                // Lỗi phía kênh chứ không phải lỗi bài: bài ở lại hàng chờ, cả hàng đợi sang ngày sau.
                logger.LogWarning("YouTube hết quota khi đăng bài {EntryId} — dừng hàng chờ tới lần quét sau", entry.Id);
                break;
            }

            var staging = videoId is null
                ? ContestEntryLifecycle.PostFailed(entry)
                : ContestEntryLifecycle.Posted(entry, videoId);

            // Lưu TRƯỚC khi xoá staging và không theo token huỷ: video đã lên YouTube mà không ghi được videoId thì
            // lần quét sau sẽ đăng trùng.
            await entryRepo.SaveChangesAsync(CancellationToken.None);
            if (videoId is not null) posted++;
            else logger.LogWarning("Bài dự thi {EntryId} không đăng được lên YouTube — chuyển Failed", entry.Id);

            await DeleteStagingAsync(staging);
        }

        return posted;
    }

    /// <summary>Ưu tiên tên người dự thi tự đặt → tên bể đã public → tên contest.</summary>
    private async Task<(string Title, string Description)> DescribeAsync(ContestEntry entry, Contest contest, CancellationToken ct)
    {
        var snapshot = await snapshotRepo.GetByIdAsync(entry.AquariumSnapshotId, ct);
        var title = entry.Title
            ?? SnapshotDataReader.TryGetAquariumName(snapshot?.SnapshotData)
            ?? contest.Title
            ?? "FishLover Contest Entry";
        var description = entry.Description ?? $"Bài dự thi {contest.Title} — The FishLover";
        return (title, description);
    }

    private async Task DeleteStagingAsync(string? key)
    {
        if (string.IsNullOrEmpty(key)) return;
        try
        {
            await storage.DeleteAsync(key, CancellationToken.None);
        }
        catch (Exception ex)
        {
            // Trạng thái đã lưu (VideoR2Key = null) nên trần staging không còn đếm file này — chỉ còn log để dọn tay.
            logger.LogError(ex, "Không xoá được video staging {Key} — cần dọn tay trên R2", key);
        }
    }
}
