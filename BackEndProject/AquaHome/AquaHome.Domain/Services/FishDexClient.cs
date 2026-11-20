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

    // Giới hạn số điểm phân bố nhúng vào JSONB mỗi loài — tránh snapshot phình to
    private const int MaxPointsPerSpecies = 200;

    public async Task<IReadOnlyDictionary<int, IReadOnlyList<DistributionPointDto>>> GetDistributionsAsync(
        IReadOnlyList<int> specCodes, CancellationToken ct = default)
    {
        var empty = new Dictionary<int, IReadOnlyList<DistributionPointDto>>();
        if (specCodes.Count == 0) return empty;

        try
        {
            var client = CreateClient();
            var codes = string.Join(",", specCodes);
            // Route public (AllowAnonymous), batch — 1 request thay vì N. Không cần JWT.
            var result = await client.GetFromJsonAsync<Dictionary<int, SpeciesDistributionResponse>>(
                $"/api/public/species/distributions?codes={codes}", ct);

            if (result is null) return empty;

            return result.ToDictionary(
                kv => kv.Key,
                kv => (IReadOnlyList<DistributionPointDto>)kv.Value.Countries
                    .SelectMany(c => c.Occurrences.Select(o =>
                        new DistributionPointDto(o.Lat, o.Lon, c.Code, o.Locality)))
                    .Take(MaxPointsPerSpecies)
                    .ToList());
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch distributions for {Count} specCodes", specCodes.Count);
            return empty;
        }
    }

    // Shape khớp SpeciesDistributionDto của FishDex (chỉ field cần dùng)
    private sealed record SpeciesDistributionResponse(List<CountryDistribution> Countries);
    private sealed record CountryDistribution(string Code, List<OccurrencePoint> Occurrences);
    private sealed record OccurrencePoint(double Lat, double Lon, string? Locality);
}
