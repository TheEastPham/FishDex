using AquaHome.Domain.Services;
using AquaHome.Domain.Services.Interfaces;
using AquaHome.Domain.Settings;
using AquaHome.EFCore.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

    public static IServiceCollection AddAquaHomeDomainServices(this IServiceCollection services)
    {
        services.AddScoped<IAquariumService, AquariumService>();
        services.AddScoped<IFavoriteService, FavoriteService>();
        services.AddScoped<IRecentlyViewedService, RecentlyViewedService>();
        services.AddScoped<IAquariumMediaService, AquariumMediaService>();
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
