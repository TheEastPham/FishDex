using AquaHome.EFCore.Repository;
using AquaHome.EFCore.Repository.Interface;
using Microsoft.Extensions.DependencyInjection;

namespace AquaHome.EFCore.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAquaHomeRepositories(this IServiceCollection services)
    {
        services.AddScoped<IAquariumRepository, AquariumRepository>();
        services.AddScoped<IUserFavoriteRepository, UserFavoriteRepository>();
        services.AddScoped<IRecentlyViewedRepository, RecentlyViewedRepository>();
        services.AddScoped<IAquariumMediaRepository, AquariumMediaRepository>();
        return services;
    }
}
