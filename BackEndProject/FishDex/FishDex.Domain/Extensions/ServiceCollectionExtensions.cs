using FishDex.Domain.Services;
using FishDex.Domain.Services.Interfaces;
using FishDex.Domain.SpeciesProfiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FishDex.Domain.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFishDexServices(this IServiceCollection services)
    {
        services.AddScoped<ISpeciesService, SpeciesService>();
        services.AddScoped<ISpeciesProfile, SpeciesProfile>();
        services.AddScoped<ICommunitySpeciesService, CommunitySpeciesService>();
        services.AddScoped<ICommunityCommonNameService, CommunityCommonNameService>();
        services.AddScoped<IOccurrenceService, OccurrenceService>();
        services.AddScoped<IMediaService, MediaService>();
        services.AddScoped<IMarketService, MarketService>();
        services.AddSingleton<IStorageService, S3StorageService>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IMediaAccessService, MediaAccessService>();
        return services;
    }
}
