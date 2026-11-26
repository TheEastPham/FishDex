using AquaHome.Domain.Services;
using AquaHome.Domain.Services.Interfaces;
using AquaHome.Domain.Settings;
using AquaHome.Domain.Storage;
using FishLover.Storage;
using AquaHome.EFCore.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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

    /// <summary>
    /// Toàn bộ domain service cho AquaHome.API: phần dùng chung với Worker (<see cref="AddAquaHomeBackgroundServices"/>)
    /// cộng các service phục vụ request. Các service này cần <c>ICurrentUserSession</c>/<c>IHttpContextAccessor</c> —
    /// chỉ có trong web host.
    /// </summary>
    public static IServiceCollection AddAquaHomeDomainServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAquaHomeBackgroundServices(configuration);

        services.AddScoped<IAquariumService, AquariumService>();
        services.AddScoped<IFavoriteService, FavoriteService>();
        services.AddScoped<IRecentlyViewedService, RecentlyViewedService>();
        services.AddScoped<IAquariumMediaService, AquariumMediaService>();
        services.AddScoped<IReminderService, ReminderService>();
        services.AddScoped<IQuotaService, QuotaService>();
        services.AddScoped<IFishDexClient, FishDexClient>();
        services.AddScoped<ISnapshotService, SnapshotService>();
        services.AddScoped<IContestService, ContestService>();
        services.AddScoped<IArticleService, ArticleService>();
        services.AddScoped<AquaHome.Domain.Articles.PublicArticleView>();

        return services;
    }

    /// <summary>
    /// Phần AquaHome.Worker cần: không phụ thuộc người dùng đang đăng nhập, nên dựng được ngoài web host. Worker chỉ gọi
    /// hàm này — gọi <see cref="AddAquaHomeDomainServices"/> thì ở môi trường Development (bật ValidateOnBuild) host
    /// không khởi động được vì thiếu <c>ICurrentUserSession</c>.
    /// </summary>
    public static IServiceCollection AddAquaHomeBackgroundServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IWebPushNotifier, WebPushNotifier>();
        services.Configure<YouTubeSettings>(configuration.GetSection(YouTubeSettings.SectionName));
        services.AddScoped<IYouTubeUploadService, YouTubeUploadService>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<AquaHome.Domain.Contests.YouTubeUploadAllowance>();
        services.AddScoped<AquaHome.Domain.Contests.ContestUploadQueue>();
        services.AddHttpClient(); // dùng cho YouTubeUploadService download video từ R2 presigned URL

        var umBaseUrl = configuration["UserManagement:BaseUrl"] ?? "http://localhost:8080";
        var internalApiKey = configuration["UserManagement:InternalApiKey"] ?? string.Empty;
        services.AddHttpClient("UserManagement", client =>
        {
            client.BaseAddress = new Uri(umBaseUrl);
            client.DefaultRequestHeaders.Add("X-Internal-Api-Key", internalApiKey);
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        var fishDexBaseUrl = configuration["FishDex:BaseUrl"] ?? "http://localhost:8081";
        services.AddHttpClient("FishDex", client =>
        {
            client.BaseAddress = new Uri(fishDexBaseUrl);
            // Dùng chung secret với luồng gọi UserManagement — một khoá duy nhất cho mọi
            // lời gọi service-to-service, nạp từ INTERNAL_API_KEY. Thiếu header này thì
            // /api/market/ingest của FishDex trả 401.
            client.DefaultRequestHeaders.Add("X-Internal-Api-Key", internalApiKey);
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        return services;
    }

    public static IServiceCollection AddAquaHomeStorage(
        this IServiceCollection services, IConfiguration configuration)
    {
        // Mặc định riêng của AquaHome — appsettings / env var Storage__* luôn thắng.
        services.AddObjectStorage(configuration, defaults: s =>
        {
            s.Provider = "r2";
            s.BucketName = "aquahome";
            s.PresignedPutUrlExpiryMinutes = 5;
        });
        services.AddSingleton<AquaHomeObjectKeys>();
        return services;
    }
}
