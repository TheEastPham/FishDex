using System.Diagnostics;
using AquaHome.Domain.Services.Interfaces;
using AquaHome.EFCore.Repository.Interface;

namespace AquaHome.Worker.Services;

/// <summary>
/// Leaderboard = YouTube viewCount. Mỗi 6h sync viewCount cho các contest đang Active qua YouTube Data API v3.
/// Không dùng Hangfire — pattern giống TaskReminderBackgroundService, Worker đã có sẵn.
/// </summary>
public class SyncContestViewCountsBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<SyncContestViewCountsBackgroundService> logger) : BackgroundService
{
    public const string ActivitySourceName = "aquahome-worker";
    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("SyncContestViewCountsBackgroundService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            await SyncViewCountsAsync(stoppingToken);
            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task SyncViewCountsAsync(CancellationToken ct)
    {
        using var activity = ActivitySource.StartActivity("contest-viewcount-sync");
        try
        {
            using var scope = scopeFactory.CreateScope();
            var entryRepo = scope.ServiceProvider.GetRequiredService<IContestEntryRepository>();
            var youTube = scope.ServiceProvider.GetRequiredService<IYouTubeUploadService>();

            var entries = await entryRepo.GetByActiveContestsAsync(ct);
            activity?.SetTag("contest.entries_count", entries.Count);

            if (entries.Count == 0)
            {
                logger.LogInformation("No active contest entries to sync");
                return;
            }

            var videoIds = entries
                .Where(e => !string.IsNullOrEmpty(e.YouTubeVideoId))
                .Select(e => e.YouTubeVideoId!)
                .Distinct()
                .ToList();

            var viewCounts = await youTube.GetViewCountsAsync(videoIds, ct);

            var updated = 0;
            foreach (var entry in entries)
            {
                if (entry.YouTubeVideoId is not null && viewCounts.TryGetValue(entry.YouTubeVideoId, out var count))
                {
                    entry.YouTubeViewCount = count;
                    updated++;
                }
            }

            await entryRepo.SaveChangesAsync(ct);
            logger.LogInformation("Synced viewCount for {Updated}/{Total} contest entries", updated, entries.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Error in SyncContestViewCountsBackgroundService");
        }
    }
}
