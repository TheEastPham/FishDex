using AquaHome.Domain.Services;
using AquaHome.Domain.Services.Interfaces;
using AquaHome.Domain.Settings;
using AquaHome.EFCore.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AquaHome.Domain.Extensions;

public static class AquaHomeServiceExtensions
{
    public static IServiceCollection AddAquaHomeServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AquaHomeDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        return services;
    }

    public static IServiceCollection AddAquaHomeDomainServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IAquariumService, AquariumService>();
        services.AddScoped<IFavoriteService, FavoriteService>();
        services.AddScoped<IRecentlyViewedService, RecentlyViewedService>();
        services.AddScoped<IAquariumMediaService, AquariumMediaService>();
        services.AddScoped<IWebPushNotifier, WebPushNotifier>();
        services.AddScoped<IReminderService, ReminderService>();

        var umBaseUrl = configuration["UserManagement:BaseUrl"] ?? "http://localhost:8080";
        var internalApiKey = configuration["UserManagement:InternalApiKey"] ?? string.Empty;
        services.AddHttpClient("UserManagement", client =>
        {
            client.BaseAddress = new Uri(umBaseUrl);
            client.DefaultRequestHeaders.Add("X-Internal-Api-Key", internalApiKey);
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        return services;
    }

    public static IServiceCollection AddAquaHomeStorage(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<StorageSettings>(configuration.GetSection(StorageSettings.SectionName));
        services.AddSingleton<IStorageService, S3StorageService>();
        return services;
    }
}
