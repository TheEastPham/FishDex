using FishDex.EFCore.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FishDex.EFCore.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Đăng ký FishDexDbContext với PostgreSQL.
    /// Gọi từ Program.cs của FishDex.API:
    ///   builder.Services.AddFishDexDatabase(builder.Configuration);
    /// </summary>
    public static IServiceCollection AddFishDexDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("FishDexDb")
                               ?? throw new InvalidOperationException(
                                   "Connection string 'FishDexDb' not found. " +
                                   "Kiểm tra appsettings.json hoặc biến môi trường.");

        services.AddDbContext<FishDexDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                // Tên assembly chứa migrations
                npgsql.MigrationsAssembly("FishDex.EFCore");

                // Retry tự động khi DB chưa sẵn sàng (hữu ích lúc Docker start)
                npgsql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorCodesToAdd: null);
            });

            // Chỉ bật detailed errors khi Development
            options.EnableDetailedErrors(
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development");
        });

        return services;
    }
}