using System.Diagnostics;
using AquaHome.Domain.Contests;
using AquaHome.Domain.Settings;
using FishLover.Storage;
using Microsoft.Extensions.Options;

namespace AquaHome.Worker.Services;

/// <summary>
/// Đăng video dự thi trong hàng chờ lên YouTube (ADR 0005). Request xác nhận chỉ kiểm file rồi trả về ngay; job này
/// mới kéo video từ R2 và đẩy lên kênh. Chạy đúng một instance Worker — hai instance sẽ đăng trùng.
/// </summary>
public class PostContestVideosBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<YouTubeSettings> youTubeSettings,
    ILogger<PostContestVideosBackgroundService> logger) : BackgroundService
{
    public const string ActivitySourceName = "aquahome-worker";
    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("PostContestVideosBackgroundService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            await PostAsync(stoppingToken);
            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task PostAsync(CancellationToken ct)
    {
        using var activity = ActivitySource.StartActivity("contest-video-post");
        try
        {
            using var scope = scopeFactory.CreateScope();
            var storage = scope.ServiceProvider.GetRequiredService<IStorageService>();

            // Thiếu credential thì mọi lần đăng đều hỏng → cả hàng chờ thành Failed. Thà để bài nằm chờ.
            if (!storage.IsConfigured || !youTubeSettings.Value.IsConfigured)
            {
                logger.LogWarning("Storage hoặc YouTube chưa cấu hình cho Worker — bỏ qua hàng chờ đăng video contest");
                return;
            }

            var queue = scope.ServiceProvider.GetRequiredService<ContestUploadQueue>();
            var posted = await queue.PostQueuedAsync(ct);
            activity?.SetTag("contest.posted_videos", posted);
            if (posted > 0) logger.LogInformation("Posted {Posted} contest videos to YouTube", posted);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Error in PostContestVideosBackgroundService");
        }
    }
}
