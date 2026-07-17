using System;
using FishDex.EFCore.Cache;
using FishDex.EFCore.DbContexts;
using FishDex.EFCore.Repository;
using FishDex.EFCore.Repository.Interface;
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

    public static IServiceCollection AddFishDexRepositories(this IServiceCollection services)
    {
        services.AddScoped<ISpeciesRepository,             SpeciesRepository>();
        services.AddScoped<IFamiliesRepository,            FamiliesRepository>();
        services.AddScoped<IGenusRepository,               GenusRepository>();
        services.AddScoped<ICommonNameRepository,          CommonNameRepository>();
        services.AddScoped<IStockRepository,               StockRepository>();
        services.AddScoped<IStockConservationRepository,   StockConservationRepository>();
        services.AddScoped<IStockEnvironmentRepository,    StockEnvironmentRepository>();
        services.AddScoped<IStockExternalRefRepository,    StockExternalRefRepository>();
        services.AddScoped<IStockDataAvailabilityRepository, StockDataAvailabilityRepository>();
        services.AddScoped<IStockMetadataRepository,       StockMetadataRepository>();
        services.AddScoped<IEcologyRepository,             EcologyRepository>();
        services.AddScoped<IHabitatZoneRepository,         HabitatZoneRepository>();
        services.AddScoped<IFeedingAndDietRepository,      FeedingAndDietRepository>();
        services.AddScoped<IAssociationsRepository,        AssociationsRepository>();
        services.AddScoped<ISubstrateRepository,           SubstrateRepository>();
        services.AddScoped<ISpecialHabitatRepository,      SpecialHabitatRepository>();
        services.AddScoped<ICircadianBehaviorRepository,   CircadianBehaviorRepository>();
        services.AddScoped<IMorphDataRepository,           MorphDataRepository>();
        services.AddScoped<IMorphTeethRepository,          MorphTeethRepository>();
        services.AddScoped<IMorphPigmentationRepository,   MorphPigmentationRepository>();
        services.AddScoped<IMorphFinsRepository,           MorphFinsRepository>();
        services.AddScoped<IMorphMeristicsRepository,      MorphMeristicsRepository>();
        services.AddScoped<IMorphMetricsRepository,        MorphMetricsRepository>();
        services.AddScoped<IOccurrenceRepository,          OccurrenceRepository>();
        services.AddScoped<IEcosystemRepository,           EcosystemRepository>();
        services.AddScoped<IEcosystemRefRepository,        EcosystemRefRepository>();
        services.AddScoped<ISystemImageRepository,         SystemImageRepository>();
        services.AddScoped<FishBaseFlattener>();
        services.AddScoped<ISpeciesCache,                  DbSpeciesCache>();
        services.AddScoped<ICommunitySpeciesRepository,    CommunitySpeciesRepository>();
        return services;
    }
}