// FishLover.Shared/Extensions/AuthorizationExtensions.cs

using System.Security.Claims;
using FishLover.Shared.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FishLover.Shared.Extensions;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddFishLoverAuthorization(
        this IServiceCollection services)
    {
        // Required dependency
        services.AddHttpContextAccessor();
        
        // Register CurrentUserSession
        services.AddScoped<ICurrentUserSession, CurrentUserSession>();
        
        // Optional: Add policies
        services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireSystemAdmin", policy =>
                policy.RequireRole("SystemAdmin"));
            
            options.AddPolicy("RequireContentAdmin", policy =>
                policy.RequireAssertion(context =>
                {
                    var roles = context.User.Claims
                        .Where(c => c.Type == ClaimTypes.Role)
                        .Select(c => c.Value)
                        .ToList();
                    return roles.Contains("SystemAdmin") || 
                           roles.Contains("ContentAdmin");
                }));
        });
        
        return services;
    }
}