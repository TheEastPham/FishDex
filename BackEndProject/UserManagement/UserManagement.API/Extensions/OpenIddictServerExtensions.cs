using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
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

                // Signing + Encryption: dùng self-signed cert persistent trên disk.
                // Cert được tạo 1 lần, lưu vào keyDir, load lại khi restart → token sống qua deploy.
                // Production: thay bằng X.509 cert thật (xem CLAUDE.md → Production checklist).
                var keyDir  = configuration["OpenIddict:KeyDir"] ?? "/tmp/openiddict-keys";
                var cert    = GetOrCreateDevCert(keyDir);
                options.AddSigningCertificate(cert);
                options.AddEncryptionCertificate(cert);
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

    private static X509Certificate2 GetOrCreateDevCert(string keyDir)
    {
        Directory.CreateDirectory(keyDir);
        var pfxPath = Path.Combine(keyDir, "openiddict-dev.pfx");

        if (File.Exists(pfxPath))
            return new X509Certificate2(pfxPath, (string?)null, X509KeyStorageFlags.PersistKeySet);

        using var rsa = RSA.Create(2048);
        var req  = new CertificateRequest("CN=OpenIddict Dev", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var cert = req.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(10));
        File.WriteAllBytes(pfxPath, cert.Export(X509ContentType.Pkcs12));
        return cert;
    }
}
