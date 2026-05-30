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
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        return services;
    }
}
