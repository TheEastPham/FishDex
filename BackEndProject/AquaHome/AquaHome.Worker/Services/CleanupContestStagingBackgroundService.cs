using System.Diagnostics;
using AquaHome.Common.Enums;
using AquaHome.Domain.Contests;
using AquaHome.EFCore.Repository.Interface;
using FishLover.Storage;

namespace AquaHome.Worker.Services;

/// <summary>
/// Dọn video contest kẹt trên R2 staging: bài tải lên rồi không xác nhận, hoặc kẹt giữa lúc kiểm file. Mỗi 6 giờ,
/// bài nào quá hạn theo <see cref="ContestEntryLifecycle.StagingExpiry"/> → chuyển Failed kèm lý do rồi xoá video.
/// Bài trong hàng chờ đã kiểm file không bị dọn — Worker sẽ đăng khi có lượt.
/// </summary>
public class CleanupContestStagingBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<CleanupContestStagingBackgroundService> logger) : BackgroundService
{
    public const string ActivitySourceName = "aquahome-worker";
    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

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
            var candidates = await entryRepo.GetWithStagingVideoAsync(now - ContestEntryLifecycle.UnconfirmedStagingLifetime, ct);
            var stale = candidates
                .Select(e => (Entry: e, Reason: ContestEntryLifecycle.StagingExpiry(e, now)))
                .Where(x => x.Reason is not null)
                .ToList();
            activity?.SetTag("contest.stale_entries", stale.Count);

            if (stale.Count == 0)
            {
                logger.LogInformation("No stale contest videos in R2 staging");
                return;
            }

            var cleaned = 0;
            foreach (var (entry, reason) in stale)
            {
                // Chuyển trạng thái trước rồi mới xoá: user vừa bấm "Xác nhận lại" thì bài đã đổi trạng thái → để yên video.
                if (!await entryRepo.TryExpireStagingAsync(entry.Id, (ContestEntryStatus)entry.Status, reason!, ct)) continue;

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
