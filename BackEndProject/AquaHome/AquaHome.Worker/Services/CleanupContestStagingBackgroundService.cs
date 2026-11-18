using System.Diagnostics;
using AquaHome.Domain.Services.Interfaces;
using AquaHome.EFCore.Repository.Interface;

namespace AquaHome.Worker.Services;

/// <summary>
/// Dọn video contest kẹt trên R2 staging. Video chỉ được xoá lúc confirm-upload xong, nên bài nào dừng giữa
/// chừng (tải lên rồi bỏ, hết lượt YouTube mà không bấm "Xác nhận lại", process chết lúc đang upload) thì video
/// nằm R2 mãi. Mỗi 6 giờ: bài Pending/Validating quá hạn → xoá video + chuyển Rejected kèm lý do để user biết
/// mà nộp lại.
/// </summary>
public class CleanupContestStagingBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<CleanupContestStagingBackgroundService> logger) : BackgroundService
{
    public const string ActivitySourceName = "aquahome-worker";
    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

    // Đã HEAD-verify (bị hoãn vì hết lượt YouTube): đủ để user quay lại bấm "Xác nhận lại" vào hôm sau.
    private static readonly TimeSpan MaxVerifiedAge = TimeSpan.FromDays(3);

    // Chưa verify (upload rồi bỏ, hoặc chưa upload gì): trần staging đã thôi tính sau 30 phút, nhưng file thật
    // (dung lượng không ai kiểm) vẫn nằm R2 → dọn sớm.
    private static readonly TimeSpan MaxUnverifiedAge = TimeSpan.FromDays(1);

    private const string ExpiredReason = "Quá 3 ngày chưa đăng được lên YouTube nên video đã bị xoá. Bạn hãy nộp lại bài.";
    private const string AbandonedReason = "Video chưa được xác nhận sau khi tải lên nên đã bị xoá. Bạn hãy nộp lại bài.";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("CleanupContestStagingBackgroundService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            await CleanupAsync(stoppingToken);
            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task CleanupAsync(CancellationToken ct)
    {
        using var activity = ActivitySource.StartActivity("contest-staging-cleanup");
        try
        {
            using var scope = scopeFactory.CreateScope();
            var storage = scope.ServiceProvider.GetRequiredService<IStorageService>();

            // Thiếu credential thì DeleteAsync chạy rỗng: chuyển Rejected lúc đó là video mồ côi trên R2 mà trần
            // staging (đếm theo VideoR2Key) không còn thấy → R2 đầy dần không ai biết. Thà không dọn.
            if (!storage.IsConfigured)
            {
                logger.LogWarning("Storage chưa cấu hình cho Worker (Storage__*) — bỏ qua dọn video contest");
                return;
            }

            var entryRepo = scope.ServiceProvider.GetRequiredService<IContestEntryRepository>();
            var now = DateTime.UtcNow;
            var stale = await entryRepo.GetStaleStagingAsync(now - MaxVerifiedAge, now - MaxUnverifiedAge, ct);
            activity?.SetTag("contest.stale_entries", stale.Count);

            if (stale.Count == 0)
            {
                logger.LogInformation("No stale contest videos in R2 staging");
                return;
            }

            var cleaned = 0;
            foreach (var entry in stale)
            {
                // Chuyển trạng thái trước rồi mới xoá: user vừa bấm "Xác nhận lại" thì bài đã đổi trạng thái → để yên video.
                var reason = entry.VideoVerifiedAt is null ? AbandonedReason : ExpiredReason;
                if (!await entryRepo.TryExpireStagingAsync(entry.Id, entry.Status, reason, ct)) continue;

                await storage.DeleteAsync(entry.VideoR2Key!, ct);
                cleaned++;
            }

            logger.LogInformation("Cleaned {Cleaned}/{Total} stale contest videos from R2 staging", cleaned, stale.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Error in CleanupContestStagingBackgroundService");
        }
    }
}
