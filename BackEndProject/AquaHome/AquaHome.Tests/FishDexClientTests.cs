using System.Net;
using AquaHome.Domain.Exceptions;
using AquaHome.Domain.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace AquaHome.Tests;

/// <summary>
/// Khoá hợp đồng lỗi của FishDexClient: "rỗng" = FishDex trả lời mà không có loài nào; mọi trường hợp
/// FishDex không trả lời được đều phải ném FishDexUnavailableException với đúng nguyên nhân.
/// </summary>
public sealed class FishDexClientTests
{
    private sealed class StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            return send(request, ct);
        }
    }

    private sealed class StubFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            return new HttpClient(handler, disposeHandler: false) { BaseAddress = new Uri("http://fishdex.test") };
        }
    }

    private static FishDexClient NewClient(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send)
    {
        return new FishDexClient(
            new StubFactory(new StubHandler(send)), new HttpContextAccessor(), NullLogger<FishDexClient>.Instance);
    }

    private static Task<HttpResponseMessage> Json(HttpStatusCode status, string body)
    {
        return Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
        });
    }

    [Fact]
    public async Task Empty_input_makes_no_call()
    {
        var client = NewClient((_, _) => throw new InvalidOperationException("không được gọi"));

        Assert.Empty(await client.GetSpeciesSummariesAsync([]));
        Assert.Empty(await client.GetDistributionsAsync([]));
    }

    [Fact]
    public async Task Empty_array_from_FishDex_is_a_valid_empty_result_not_an_error()
    {
        var client = NewClient((_, _) => Json(HttpStatusCode.OK, "[]"));

        Assert.Empty(await client.GetSpeciesSummariesAsync([1]));
    }

    [Fact]
    public async Task Summaries_are_parsed()
    {
        var client = NewClient((_, _) => Json(HttpStatusCode.OK,
            """[{"specCode":7,"speciesName":"Oryzias latipes","commonName":"Medaka","imageKey":"7/a.jpg"}]"""));

        var result = await client.GetSpeciesSummariesAsync([7]);

        var s = Assert.Single(result);
        Assert.Equal("Oryzias latipes", s.SpeciesName);
        Assert.Equal("7/a.jpg", s.ImageKey);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Unauthorized)]
    public async Task Non_success_status_throws_BadStatus_with_the_code(HttpStatusCode status)
    {
        var client = NewClient((_, _) => Json(status, "{}"));

        var ex = await Assert.ThrowsAsync<FishDexUnavailableException>(() => client.GetSpeciesSummariesAsync([1]));

        Assert.Equal(FishDexFailure.BadStatus, ex.Failure);
        Assert.Equal((int)status, ex.StatusCode);
    }

    [Fact]
    public async Task Connection_failure_throws_Unreachable()
    {
        var client = NewClient((_, _) => throw new HttpRequestException("connection refused"));

        var ex = await Assert.ThrowsAsync<FishDexUnavailableException>(() => client.GetDistributionsAsync([1]));

        Assert.Equal(FishDexFailure.Unreachable, ex.Failure);
    }

    [Fact]
    public async Task HttpClient_timeout_throws_Timeout()
    {
        // HttpClient.Timeout bắn TaskCanceledException trong khi token của caller chưa bị hủy.
        var client = NewClient((_, _) => throw new TaskCanceledException("timeout"));

        var ex = await Assert.ThrowsAsync<FishDexUnavailableException>(() => client.GetSpeciesSummariesAsync([1]));

        Assert.Equal(FishDexFailure.Timeout, ex.Failure);
    }

    [Fact]
    public async Task Malformed_body_throws_InvalidResponse()
    {
        var client = NewClient((_, _) => Json(HttpStatusCode.OK, "{ not json"));

        var ex = await Assert.ThrowsAsync<FishDexUnavailableException>(() => client.GetSpeciesSummariesAsync([1]));

        Assert.Equal(FishDexFailure.InvalidResponse, ex.Failure);
    }

    [Fact]
    public async Task Caller_cancellation_is_not_reported_as_FishDex_failure()
    {
        using var cts = new CancellationTokenSource();
        var client = NewClient((_, ct) =>
        {
            cts.Cancel();
            ct.ThrowIfCancellationRequested();
            return Json(HttpStatusCode.OK, "[]");
        });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.GetSpeciesSummariesAsync([1], cts.Token));
    }

    [Fact]
    public async Task Distributions_are_flattened_and_capped_per_species()
    {
        var occurrences = string.Join(",", Enumerable.Range(0, 250).Select(i => $$"""{"lat":1.5,"lon":2.5,"locality":"p{{i}}"}"""));
        var body = "{\"7\":{\"countries\":[{\"code\":\"VN\",\"occurrences\":[" + occurrences + "]}]}}";
        var client = NewClient((_, _) => Json(HttpStatusCode.OK, body));

        var result = await client.GetDistributionsAsync([7]);

        Assert.Equal(200, result[7].Count);
        Assert.All(result[7], p => Assert.Equal("VN", p.CountryCode));
    }
}
