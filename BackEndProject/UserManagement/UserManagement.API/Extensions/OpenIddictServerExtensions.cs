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

                // Signing: BẮT BUỘC asymmetric (RSA) theo OIDC spec để ký ID token.
                // Ephemeral RSA vẫn ổn vì access token ngắn hạn (60 phút), FE tự refresh.
                options.AddEphemeralSigningKey();

                // Encryption: dùng fixed symmetric key từ config để refresh token
                // sống qua container restart (ephemeral encryption key là nguyên nhân F5 lỗi).
                // Production: thay bằng X.509 cert (xem CLAUDE.md → Production checklist).
                var secret = configuration["JwtSettings:SecretKey"]
                    ?? throw new InvalidOperationException("JwtSettings:SecretKey not configured");
                var encBytes = Encoding.UTF8.GetBytes((secret + "_oidc_enc").PadRight(32)[..32]);
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
