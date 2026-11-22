using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AquaHome.Domain.DTOs;
using AquaHome.Domain.Exceptions;
using AquaHome.Domain.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AquaHome.Domain.Services;

/// <summary>
/// Gọi FishDex.API để lấy species summaries + distribution points khi gen snapshot preview/publish.
/// Lỗi → <see cref="FishDexUnavailableException"/>; "không có dữ liệu" → rỗng.
/// </summary>
public class FishDexClient(
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContextAccessor,
    ILogger<FishDexClient> logger) : IFishDexClient
{
    // Giới hạn số điểm phân bố nhúng vào JSONB mỗi loài — tránh snapshot phình to
    private const int MaxPointsPerSpecies = 200;

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

        // Route public (AllowAnonymous) — snapshot public được xem bởi người chưa login (không có JWT để forward).
        var result = await GetAsync<List<SpeciesSummaryDto>>(
            $"/api/public/species/summaries?codes={string.Join(",", specCodes)}", ct);
        return result ?? [];
    }

    public async Task<IReadOnlyDictionary<int, IReadOnlyList<DistributionPointDto>>> GetDistributionsAsync(
        IReadOnlyList<int> specCodes, CancellationToken ct = default)
    {
        var empty = new Dictionary<int, IReadOnlyList<DistributionPointDto>>();
        if (specCodes.Count == 0) return empty;

        // Route public (AllowAnonymous), batch — 1 request thay vì N. Không cần JWT.
        var result = await GetAsync<Dictionary<int, SpeciesDistributionResponse>>(
            $"/api/public/species/distributions?codes={string.Join(",", specCodes)}", ct);

        if (result is null) return empty;

        return result.ToDictionary(
            kv => kv.Key,
            kv => (IReadOnlyList<DistributionPointDto>)kv.Value.Countries
                .SelectMany(c => c.Occurrences.Select(o =>
                    new DistributionPointDto(o.Lat, o.Lon, c.Code, o.Locality)))
                .Take(MaxPointsPerSpecies)
                .ToList());
    }

    /// <summary>
    /// GET + đọc JSON. Mọi thất bại (mạng, timeout, non-2xx, body hỏng) đều ném <see cref="FishDexUnavailableException"/>
    /// kèm nguyên nhân — không bao giờ trả giá trị rỗng thay cho lỗi. Chỉ hủy do caller (ct) mới ném OperationCanceledException.
    /// </summary>
    private async Task<T?> GetAsync<T>(string path, CancellationToken ct)
    {
        var route = PathOnly(path);
        try
        {
            using var response = await CreateClient().GetAsync(path, ct);
            if (!response.IsSuccessStatusCode)
            {
                var status = (int)response.StatusCode;
                logger.LogWarning("FishDex {Path} trả {Status} ({Kind})", route, status, status >= 500 ? "FishDex lỗi" : "lệch hợp đồng/route");
                throw new FishDexUnavailableException(FishDexFailure.BadStatus, $"FishDex trả HTTP {status}", status);
            }

            return await response.Content.ReadFromJsonAsync<T>(ct);
        }
        catch (FishDexUnavailableException)
        {
            throw;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException ex) // không phải caller hủy → HttpClient.Timeout
        {
            logger.LogWarning(ex, "FishDex {Path} quá thời hạn", route);
            throw new FishDexUnavailableException(FishDexFailure.Timeout, "FishDex quá thời hạn", inner: ex);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "FishDex {Path} không kết nối được", route);
            throw new FishDexUnavailableException(FishDexFailure.Unreachable, "Không kết nối được FishDex", inner: ex);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "FishDex {Path} trả body không đúng shape", route);
            throw new FishDexUnavailableException(FishDexFailure.InvalidResponse, "FishDex trả dữ liệu không hợp lệ", inner: ex);
        }
    }

    private static string PathOnly(string path)
    {
        var q = path.IndexOf('?');
        return q < 0 ? path : path[..q];
    }

    // Shape khớp SpeciesDistributionDto của FishDex (chỉ field cần dùng)
    private sealed record SpeciesDistributionResponse(List<CountryDistribution> Countries);
    private sealed record CountryDistribution(string Code, List<OccurrencePoint> Occurrences);
    private sealed record OccurrencePoint(double Lat, double Lon, string? Locality);
}
