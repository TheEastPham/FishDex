using FishDex.Domain.Services;
using FishDex.Domain.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FishDex.Domain.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFishDexServices(this IServiceCollection services)
    {
        services.AddScoped<ISpeciesService, SpeciesService>();
        services.AddScoped<ICommunitySpeciesService, CommunitySpeciesService>();
        services.AddScoped<IEcologyService, EcologyService>();
        services.AddScoped<IStockService, StockService>();
        services.AddScoped<IMorphDataService, MorphDataService>();
        services.AddScoped<IEcosystemService, EcosystemService>();
        services.AddScoped<IOccurrenceService, OccurrenceService>();
        services.AddScoped<IMediaService, MediaService>();
        services.AddSingleton<IStorageService, S3StorageService>();
        return services;
    }
}
