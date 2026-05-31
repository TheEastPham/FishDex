using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;

namespace UserManagement.API.Extensions;

public static class OpenIddictServerExtensions
{
    public static IServiceCollection AddOpenIddictServer(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOpenIddict()
            .AddServer(options =>
            {
                options.SetAuthorizationEndpointUris("/connect/authorize")
                       .SetTokenEndpointUris("/connect/token")
                       .SetUserinfoEndpointUris("/connect/userinfo")
                       .SetLogoutEndpointUris("/connect/logout")
                       .SetRevocationEndpointUris("/connect/revoke");

                options.AllowAuthorizationCodeFlow()
                       .RequireProofKeyForCodeExchange()
                       .AllowRefreshTokenFlow();

                options.RegisterScopes(
                    OpenIddictConstants.Scopes.OpenId,
                    OpenIddictConstants.Scopes.Email,
                    OpenIddictConstants.Scopes.Profile,
                    OpenIddictConstants.Scopes.Roles,
                    "fishdex");

                // Keys: dùng fixed key từ config để token sống qua container restart.
                // Production: thay bằng X.509 cert (xem CLAUDE.md → Production checklist).
                var secret = configuration["JwtSettings:SecretKey"]
                    ?? throw new InvalidOperationException("JwtSettings:SecretKey not configured");
                var keyBytes = Encoding.UTF8.GetBytes(secret.PadRight(32)[..32]);

                // Signing: ký token (access token, refresh token)
                options.AddSigningKey(new SymmetricSecurityKey(keyBytes));
                // Encryption: mã hoá refresh token — dùng key đủ 256-bit
                var encBytes = Encoding.UTF8.GetBytes((secret + "_enc").PadRight(32)[..32]);
                options.AddEncryptionKey(new SymmetricSecurityKey(encBytes));
                options.DisableAccessTokenEncryption();

                // Cố định issuer để OIDC discovery và token iss nhất quán dù request đến từ browser hay Docker internal
                var issuer = configuration["OpenIddict:Issuer"];
                if (!string.IsNullOrEmpty(issuer))
                    options.SetIssuer(new Uri(issuer));

                options.UseAspNetCore()
                       .EnableAuthorizationEndpointPassthrough()
                       .EnableTokenEndpointPassthrough()
                       .EnableLogoutEndpointPassthrough()
                       .EnableUserinfoEndpointPassthrough()
                       .DisableTransportSecurityRequirement();
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        return services;
    }
}
