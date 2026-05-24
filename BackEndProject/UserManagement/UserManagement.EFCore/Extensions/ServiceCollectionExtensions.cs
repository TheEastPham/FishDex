using UserManagement.EFCore.Data;
using UserManagement.EFCore.Repositories;
using UserManagement.EFCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace UserManagement.EFCore.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUserManagementEFCore(this IServiceCollection services, IConfiguration configuration)
    {
        // Add DbContext
        services.AddDbContext<UserManagementDbContext>(options =>
        {
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            options.UseOpenIddict();
        });

        // Add Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IInvitationRepository, InvitationRepository>();
        services.AddScoped<IInvitationUsedRepository, InvitationUsedRepository>();

        // AddCore ở đây vì cần DbContext — AddServer/AddValidation ở API layer
        services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                       .UseDbContext<UserManagementDbContext>();
            });

        return services;
    }
}
