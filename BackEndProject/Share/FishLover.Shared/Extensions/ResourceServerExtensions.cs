using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace FishLover.Shared.Extensions;

/// <summary>
/// Bootstrap resource server dùng chung cho FishDex và AquaHome: chấp nhận cả token direct-login
/// (HS256, scheme "Bearer") lẫn token OAuth2 PKCE do OpenIddict phát (scheme "OpenIddict", JWKS discovery),
/// và đăng ký các policy admin chạy trên cả hai scheme + khoá nội bộ.
/// </summary>
public static class ResourceServerExtensions
{
    public const string OpenIddictScheme = "OpenIddict";

    public static IServiceCollection AddFishLoverResourceServer(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddFishLoverJwtAuthentication(configuration);

        var publicUrl   = configuration["AuthServer:Url"] ?? "http://localhost:8080";
        var internalUrl = configuration["AuthServer:Authority"] ?? publicUrl;
        var issuer      = publicUrl.TrimEnd('/');

        services.AddAuthentication()
            .AddJwtBearer(OpenIddictScheme, options =>
            {
                options.MetadataAddress = $"{internalUrl}/.well-known/openid-configuration";
                options.RequireHttpsMetadata = false;
                // Tắt mapping mặc định: .NET đổi "role" → URI dài nên RoleClaimType="role" sẽ không khớp.
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    // OpenIddict thêm trailing slash vào issuer — chấp nhận cả hai dạng.
                    ValidIssuers = [issuer, issuer + "/"],
                    ValidateIssuer = true,
                    // TODO(prod #4): đăng ký resource server trong OpenIddict rồi bật validate audience.
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    RoleClaimType = "role",
                    NameClaimType = "name",
                };
            })
            .AddFishLoverInternalApiKey();

        services.AddFishLoverAuthorization();

        // Policy shared không khai scheme → chỉ chạy trên default; đăng ký đè với cả hai scheme.
        services.AddAuthorization(options =>
        {
            string[] userSchemes = [JwtBearerDefaults.AuthenticationScheme, OpenIddictScheme];

            options.DefaultPolicy = new AuthorizationPolicyBuilder(userSchemes)
                .RequireAuthenticatedUser()
                .Build();

            options.AddPolicy("RequireSystemAdmin", p => p
                .AddAuthenticationSchemes(userSchemes)
                .RequireRole("SystemAdmin"));

            options.AddPolicy("RequireContentAdmin", p => p
                .AddAuthenticationSchemes(userSchemes)
                .RequireRole("SystemAdmin", "ContentAdmin"));
        });

        return services;
    }
}
