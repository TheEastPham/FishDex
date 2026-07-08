using System.Net.Http.Headers;
using System.Net.Http.Json;
using AquaHome.Domain.DTOs;
using AquaHome.Domain.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AquaHome.Domain.Services;

/// <summary>Gọi FishDex.API để lấy species summaries + distribution points khi gen snapshot preview/publish.</summary>
public class FishDexClient(
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContextAccessor,
    ILogger<FishDexClient> logger) : IFishDexClient
{
    private HttpClient CreateClient()
    {
        var client = httpClientFactory.CreateClient("FishDex");

        // Forward JWT của user hiện tại — FishDex.API endpoints yêu cầu [Authorize]
        var authHeader = httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrEmpty(authHeader) && AuthenticationHeaderValue.TryParse(authHeader, out var parsed))
            client.DefaultRequestHeaders.Authorization = parsed;

        return client;
    }

    public async Task<IReadOnlyList<SpeciesSummaryDto>> GetSpeciesSummariesAsync(
        IReadOnlyList<int> specCodes, CancellationToken ct = default)
    {
        if (specCodes.Count == 0) return [];

        try
        {
            var client = CreateClient();
            var codes = string.Join(",", specCodes);
            // Route public (AllowAnonymous) — snapshot public được xem bởi người chưa login (không có JWT để forward).
            var result = await client.GetFromJsonAsync<List<SpeciesSummaryDto>>(
                $"/api/public/species/summaries?codes={codes}", ct);
            return result ?? [];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch species summaries for {Count} specCodes", specCodes.Count);
            return [];
        }
    }

    public async Task<IReadOnlyList<DistributionPointDto>> GetOccurrencesAsync(int specCode, CancellationToken ct = default)
    {
        try
        {
            var client = CreateClient();
            var result = await client.GetFromJsonAsync<List<DistributionPointDto>>(
                $"/api/species/{specCode}/occurrences?limit=200", ct);
            return result ?? [];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch occurrences for specCode {SpecCode}", specCode);
            return [];
        }
    }
}
