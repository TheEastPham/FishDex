using System.Net;
using FishLover.Shared.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FishLover.Shared.Tests;

public class InternalApiKeyTests
{
    private static async Task<HttpClient> StartAsync(string? configuredKey)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { [InternalApiKey.ConfigKey] = configuredKey });
        builder.Services.AddAuthentication().AddFishLoverInternalApiKey();

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGet("/internal", () => "ok").RequireAuthorization(InternalApiKey.PolicyName);
        await app.StartAsync();
        return app.GetTestClient();
    }

    private static async Task<HttpStatusCode> CallAsync(string? configuredKey, string? sentKey)
    {
        var client = await StartAsync(configuredKey);
        var req = new HttpRequestMessage(HttpMethod.Get, "/internal");
        if (sentKey != null) req.Headers.Add(InternalApiKey.HeaderName, sentKey);
        return (await client.SendAsync(req)).StatusCode;
    }

    [Fact]
    public async Task Correct_key_is_accepted() => Assert.Equal(HttpStatusCode.OK, await CallAsync("s3cret", "s3cret"));

    [Fact]
    public async Task Wrong_key_is_rejected() => Assert.Equal(HttpStatusCode.Unauthorized, await CallAsync("s3cret", "nope"));

    [Fact]
    public async Task Missing_header_is_rejected() => Assert.Equal(HttpStatusCode.Unauthorized, await CallAsync("s3cret", null));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("REPLACE_WITH_RANDOM_SECRET_KEY")]
    public async Task Unconfigured_key_rejects_everything_even_a_matching_header(string? configured)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, await CallAsync(configured, configured ?? ""));
    }
}
