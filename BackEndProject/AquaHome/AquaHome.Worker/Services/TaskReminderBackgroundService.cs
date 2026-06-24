using AquaHome.Common.Enums;
using AquaHome.Domain.Services.Interfaces;
using AquaHome.EFCore.Repository.Interface;

namespace AquaHome.Worker.Services;

public class TaskReminderBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<TaskReminderBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan LookaheadWindow = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("TaskReminderBackgroundService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessDueTasksAsync(stoppingToken);
            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task ProcessDueTasksAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var taskRepo = scope.ServiceProvider.GetRequiredService<IAquariumTaskRepository>();
            var pushNotifier = scope.ServiceProvider.GetRequiredService<IWebPushNotifier>();

            var cutoff = DateTime.UtcNow.Add(LookaheadWindow);
            var dueTasks = await taskRepo.GetDueUnremindedAsync(cutoff, ct);

            if (dueTasks.Count == 0) return;

            logger.LogInformation("Sending reminders for {Count} due tasks", dueTasks.Count);

            var sendTasks = dueTasks.Select(t =>
            {
                var title = t.AquaTaskType == AquaTaskType.WaterChange ? "Đến giờ thay nước!" : "Đến giờ vệ sinh lọc!";
                return pushNotifier.SendAsync(t.UserId, title, t.Aquarium?.Name ?? "Hồ cá", "/tasks", ct);
            });

            await Task.WhenAll(sendTasks);
            await taskRepo.MarkRemindedAsync(dueTasks.Select(t => t.Id), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Error in TaskReminderBackgroundService");
        }
    }
}
