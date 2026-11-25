using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FishLover.Shared.Extensions;

public static class DatabaseMigrationExtensions
{
    /// <summary>
    /// Cờ bật auto-migrate khi khởi động. Chỉ dành cho local/Docker dev; production nên tắt và
    /// chạy <c>dotnet ef database update</c> thành bước riêng (checklist prod #3 trong BackEndProject/CLAUDE.md).
    /// </summary>
    public const string AutoMigrateFlag = "AutoMigrate:OnStartup";

    /// <summary>
    /// Chạy EF Core migration cho <typeparamref name="TContext"/> nếu <c>AutoMigrate:OnStartup=true</c>; tắt thì không làm gì.
    /// Gọi sau khi build host, trước khi Run. Migration lỗi thì ném ra để host dừng, không chạy trên schema lệch.
    /// Dùng được cho cả WebApplication lẫn worker (đều là <see cref="IHost"/>).
    /// </summary>
    public static async Task MigrateDatabaseIfEnabledAsync<TContext>(this IHost host, CancellationToken ct = default)
        where TContext : DbContext
    {
        var configuration = host.Services.GetRequiredService<IConfiguration>();
        if (!configuration.GetValue<bool>(AutoMigrateFlag)) return;

        using var scope = host.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<TContext>().Database.MigrateAsync(ct);

        host.Services.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(DatabaseMigrationExtensions))
            .LogInformation("Database migration completed for {DbContext}", typeof(TContext).Name);
    }
}
