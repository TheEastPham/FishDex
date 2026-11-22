using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FishLover.Shared.Extensions;

/// <summary>
/// Xác thực lời gọi service-to-service bằng header <c>X-Internal-Api-Key</c>, so với
/// <c>InternalSettings:ApiKey</c> (nạp từ <c>INTERNAL_API_KEY</c>). Dùng qua
/// <c>[Authorize(Policy = InternalApiKey.PolicyName)]</c> thay cho việc controller tự so chuỗi.
/// </summary>
public static class InternalApiKey
{
    public const string SchemeName = "InternalApiKey";
    public const string PolicyName = "InternalService";
    public const string HeaderName = "X-Internal-Api-Key";
    public const string ConfigKey  = "InternalSettings:ApiKey";

    private const string PlaceholderPrefix = "REPLACE_WITH";

    public static AuthenticationBuilder AddFishLoverInternalApiKey(this AuthenticationBuilder builder)
    {
        builder.Services.AddAuthorization(options =>
            options.AddPolicy(PolicyName, p => p
                .AddAuthenticationSchemes(SchemeName)
                .RequireAuthenticatedUser()));

        return builder.AddScheme<AuthenticationSchemeOptions, InternalApiKeyHandler>(SchemeName, null);
    }

    /// <summary>Fail-closed: khoá rỗng hoặc còn là placeholder trong appsettings = chưa cấu hình.</summary>
    internal static bool IsConfigured(string? key)
    {
        return !string.IsNullOrWhiteSpace(key) && !key.StartsWith(PlaceholderPrefix, StringComparison.Ordinal);
    }

    /// <summary>So sánh hằng-thời-gian trên hash SHA-256 (độ dài cố định, không lộ độ dài khoá).</summary>
    internal static bool Matches(string expected, string provided)
    {
        return CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(expected)),
            SHA256.HashData(Encoding.UTF8.GetBytes(provided)));
    }
}

internal sealed class InternalApiKeyHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration config) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var expected = config[InternalApiKey.ConfigKey];
        if (!InternalApiKey.IsConfigured(expected))
        {
            Logger.LogError("{Config} chưa được cấu hình — từ chối mọi lời gọi nội bộ.", InternalApiKey.ConfigKey);
            return Task.FromResult(AuthenticateResult.Fail("Internal API key not configured."));
        }

        if (!Request.Headers.TryGetValue(InternalApiKey.HeaderName, out var provided) || provided.Count != 1
            || !InternalApiKey.Matches(expected!, provided[0]!))
        {
            Logger.LogWarning("Lời gọi nội bộ bị từ chối: thiếu hoặc sai {Header}.", InternalApiKey.HeaderName);
            return Task.FromResult(AuthenticateResult.Fail("Invalid internal API key."));
        }

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "internal-service")], Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}
