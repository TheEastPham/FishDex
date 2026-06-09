using AquaHome.Domain.Services;
using AquaHome.Domain.Services.Interfaces;
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
        return services;
    }
}
