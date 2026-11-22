using FishDex.Domain.Storage;

namespace FishDex.API.BackgroundServices;

/// <summary>
/// Chạy <see cref="CommunityImageJanitor"/> định kỳ: lần đầu sau khi API lên được một lúc (không tranh tài nguyên lúc
/// khởi động/migrate), sau đó mỗi <see cref="Interval"/>. Lỗi một lượt chỉ ghi log, lượt sau chạy tiếp.
/// </summary>
/// <remarks>FishDex chạy 1 instance trên VM1; nếu sau này nhân bản thì mỗi instance cùng quét — vô hại vì xoá là idempotent.</remarks>
public sealed class CommunityImageCleanupService(IServiceScopeFactory scopes, ILogger<CommunityImageCleanupService> logger) : BackgroundService
{
    private static readonly TimeSpan FirstRunDelay = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(FirstRunDelay, stoppingToken);

            using var timer = new PeriodicTimer(Interval);
            do
            {
                await RunOnceAsync(stoppingToken);
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // API đang tắt.
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        try
        {
            // Janitor dùng repository → DbContext scoped, nên mỗi lượt một scope.
            await using var scope = scopes.CreateAsyncScope();
            var janitor = scope.ServiceProvider.GetRequiredService<CommunityImageJanitor>();
            await janitor.RunAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Community image cleanup failed — will retry next interval");
        }
    }
}
