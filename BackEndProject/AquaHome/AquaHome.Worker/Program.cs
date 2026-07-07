using AquaHome.Domain.Extensions;
using AquaHome.EFCore.Data;
using AquaHome.EFCore.Extensions;
using AquaHome.Worker.Services;
using FishLover.Shared.Extensions;
using Microsoft.EntityFrameworkCore;
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

var host = builder.Build();

if (builder.Configuration.GetValue<bool>("AutoMigrate:OnStartup"))
{
    using var scope = host.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AquaHomeDbContext>();
    await db.Database.MigrateAsync();
    Log.Information("Database migration completed");
}

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
