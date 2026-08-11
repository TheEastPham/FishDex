using System.Diagnostics;
using System.Net.Http.Json;
using AquaHome.EFCore.Repository.Interface;

namespace AquaHome.Worker.Services;

/// <summary>
/// Đẩy dữ liệu loài trong bể sang lớp market của FishDex.
///
/// <para>Suy luận một chiều: bể ở quốc gia X có cá Z nghĩa là quốc gia X bán cá Z. Không ai
/// nhập loài ngoại lai lẻ tẻ ở quy mô cá nhân, nên cá có mặt trong bể ở nước đó thì nó đã
/// được bán ở đó.</para>
///
/// <para><b>Không gửi bất kỳ thông tin user hay bể nào</b> — chỉ cặp quốc gia và mã loài.
/// Không được truy ngược từ danh sách market về chủ bể.</para>
///
/// <para>Chạy nền theo chu kỳ chứ KHÔNG gọi lúc user thêm cá, để độ trễ của thao tác thêm cá
/// không phụ thuộc tình trạng FishDex.</para>
/// </summary>
public class SyncMarketSpeciesBackgroundService(
    IServiceScopeFactory scopeFactory,
    IHttpClientFactory httpClientFactory,
    ILogger<SyncMarketSpeciesBackgroundService> logger) : BackgroundService
{
    public const string ActivitySourceName = "aquahome-worker";
    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    /// <summary>
    /// Dữ liệu bể đổi rất chậm — người ta không thêm cá mỗi giờ — nên 8 tiếng là đủ dày.
    /// Gửi lại toàn bộ mỗi lần thay vì gửi phần thay đổi: endpoint ingest idempotent theo khoá
    /// (CountryCode, SpecCode) nên gửi lại không sinh rác, mà tự sửa được nếu có lần chạy lỗi
    /// giữa chừng — và bỏ luôn nhu cầu lưu mốc thời gian lần chạy cuối.
    /// </summary>
    private static readonly TimeSpan Interval = TimeSpan.FromHours(8);

    private const string IngestPath = "/api/market/ingest";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("SyncMarketSpeciesBackgroundService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            await SyncAsync(stoppingToken);
            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task SyncAsync(CancellationToken ct)
    {
        using var activity = ActivitySource.StartActivity("market-species-sync");
        try
        {
            using var scope = scopeFactory.CreateScope();
            var aquariumRepo = scope.ServiceProvider.GetRequiredService<IAquariumRepository>();

            var byCountry = await aquariumRepo.GetSpecCodesByCountryAsync(ct);
            activity?.SetTag("market.country_count", byCountry.Count);

            if (byCountry.Count == 0)
            {
                logger.LogInformation("Market sync: chưa có bể nào gắn quốc gia, bỏ qua");
                return;
            }

            var client = httpClientFactory.CreateClient("FishDex");

            foreach (var (countryCode, specCodes) in byCountry)
            {
                await PushCountryAsync(client, countryCode, specCodes, ct);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Error in SyncMarketSpeciesBackgroundService");
        }
    }

    /// <summary>
    /// Một nước lỗi thì không kéo theo các nước còn lại — bắt lỗi trong vòng lặp chứ không
    /// để nó thoát ra ngoài.
    /// </summary>
    private async Task PushCountryAsync(
        HttpClient client, string countryCode, IReadOnlyList<int> specCodes, CancellationToken ct)
    {
        try
        {
            // FishDex nhận alpha-2 ở API, còn bể lưu C_Code dạng số giống dữ liệu FishBase.
            var alpha2 = MarketCountryCodes.ToAlpha2(countryCode);
            if (alpha2 is null)
            {
                logger.LogWarning(
                    "Market sync: bỏ qua mã quốc gia {CountryCode} — không nằm trong danh sách market",
                    countryCode);
                return;
            }

            var payload = new { countryAlpha2 = alpha2, specCodes };
            var response = await client.PostAsJsonAsync(IngestPath, payload, ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Market sync {Country}: FishDex trả {Status} — {Reason}",
                    alpha2, (int)response.StatusCode, response.ReasonPhrase);
                return;
            }

            var result = await response.Content.ReadFromJsonAsync<IngestResult>(ct);
            logger.LogInformation(
                "Market sync {Country}: gửi {Sent} loài, FishDex thêm mới {Added}",
                alpha2, specCodes.Count, result?.Added ?? 0);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Market sync lỗi ở quốc gia {CountryCode}", countryCode);
        }
    }

    private sealed record IngestResult(int Received, int Added);
}

/// <summary>
/// Đổi C_Code (giá trị lưu trong bể, khớp dữ liệu FishBase) sang alpha-2 mà API market nhận.
/// Cố ý chỉ liệt kê các nước có market — mã lạ thì bỏ qua kèm cảnh báo, không đoán.
/// Danh sách này phải khớp <c>MarketCountries</c> bên FishDex.
/// </summary>
internal static class MarketCountryCodes
{
    private static readonly Dictionary<string, string> Map = new()
    {
        ["704"] = "VN", ["840"] = "US", ["156"] = "CN", ["392"] = "JP",
        ["528"] = "NL", ["276"] = "DE", ["826"] = "GB", ["356"] = "IN",
        ["458"] = "MY", ["702"] = "SG", ["764"] = "TH", ["360"] = "ID",
    };

    public static string? ToAlpha2(string? code)
        => code is not null && Map.TryGetValue(code.Trim(), out var alpha2) ? alpha2 : null;
}
