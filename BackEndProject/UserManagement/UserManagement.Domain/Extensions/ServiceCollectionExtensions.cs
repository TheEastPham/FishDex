using UserManagement.Domain.Services;
using UserManagement.Domain.Services.Interfaces;
using UserManagement.EFCore.Data;
using UserManagement.EFCore.Entities.User;
using UserManagement.EFCore.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UserManagement.Domain.Helper;

namespace UserManagement.Domain.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUserManagementDomain(this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add EFCore layer
        services.AddUserManagementEFCore(configuration);

        // Add Identity
        services.AddIdentity<UserEntity, RoleEntity>(options =>
            {
                // Password settings
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 8;
                options.Password.RequiredUniqueChars = 1;

                // Lockout settings
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                // User settings
                options.User.AllowedUserNameCharacters =
                    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
                options.User.RequireUniqueEmail = true;

                // SignIn settings
                options.SignIn.RequireConfirmedEmail = false;
                options.SignIn.RequireConfirmedPhoneNumber = false;
            }).AddEntityFrameworkStores<UserManagementDbContext>()
            .AddDefaultTokenProviders();

        // Email Services
        services.AddSingleton<EmailTemplateHelper>();
        
        services.AddHttpClient("ResendClient", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "FishLover-EmailService/1.0");
        });

        // Note: Domain services (IUserService, IAuthService, etc.) are now registered via Autofac
        // See UserManagementModule for auto-registration using conventions

        return services;
    }
}