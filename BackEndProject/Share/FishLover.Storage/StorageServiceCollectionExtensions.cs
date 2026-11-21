using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FishLover.Storage;

public static class StorageServiceCollectionExtensions
{
    /// <summary>
    /// Đăng ký <see cref="IStorageService"/> (S3/R2, singleton) đọc section <c>Storage</c>.
    /// <paramref name="defaults"/> đặt mặc định riêng của service (bucket, thời hạn PUT…) — chạy TRƯỚC khi bind
    /// config, nên appsettings/env var luôn thắng.
    /// </summary>
    public static IServiceCollection AddObjectStorage(this IServiceCollection services, IConfiguration configuration, Action<StorageSettings>? defaults = null)
    {
        if (defaults is not null)
        {
            services.Configure(defaults);
        }

        services.Configure<StorageSettings>(configuration.GetSection(StorageSettings.SectionName));
        services.AddSingleton<IStorageService, S3StorageService>();
        return services;
    }
}
