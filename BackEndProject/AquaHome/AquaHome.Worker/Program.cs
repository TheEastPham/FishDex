using AquaHome.Domain.Extensions;
using AquaHome.EFCore.Data;
using AquaHome.EFCore.Extensions;
using AquaHome.Worker.Services;
using FishLover.Shared.Extensions;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Services.AddSerilog();
builder.Services.AddAquaHomeServices(builder.Configuration);
builder.Services.AddAquaHomeRepositories();
builder.Services.AddAquaHomeDomainServices(builder.Configuration);
builder.Services.AddAquaHomeStorage(builder.Configuration);

// OpenTelemetry tracing nhẹ (HttpClient + EFCore + OTLP) → trace vòng nhắc lịch end-to-end
builder.Services.AddFishLoverWorkerTracing(builder.Configuration, TaskReminderBackgroundService.ActivitySourceName);

builder.Services.AddHostedService<TaskReminderBackgroundService>();
builder.Services.AddHostedService<SyncContestViewCountsBackgroundService>();
builder.Services.AddHostedService<SyncMarketSpeciesBackgroundService>();
builder.Services.AddHostedService<CleanupContestStagingBackgroundService>();
builder.Services.AddHostedService<PostContestVideosBackgroundService>();

var host = builder.Build();

await host.MigrateDatabaseIfEnabledAsync<AquaHomeDbContext>();

try
{
    Log.Information("Starting AquaHome.Worker");
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Worker terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
