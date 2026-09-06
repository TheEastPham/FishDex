using System.Security.Cryptography;
using System.Text;
using FishDex.Domain.Services.Interfaces;
using FishDex.Domain.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace FishDex.Domain.Services;

/// <summary>
/// Hạn mức xem loài của khách, đếm bằng Redis SET: mỗi specCode là một phần tử, nên xem lại loài cũ
/// trong ngày không tốn thêm lượt và SCARD cho ra số lượt đã dùng — không cần bảng, không cần cron dọn.
/// </summary>
public class AnonQuotaService(
    IConnectionMultiplexer? redis,
    IOptions<AnonQuotaSettings> options,
    ILogger<AnonQuotaService> logger) : IAnonQuotaService
{
    private const int KeyTtlSeconds = 36 * 60 * 60;

    /// <summary>
    /// Kiểm-tra-rồi-thêm phải nằm trong MỘT lệnh Redis. Một trang profile bắn 4 request song song
    /// cho cùng specCode; nếu tách SISMEMBER/SCARD/SADD thành nhiều round-trip thì chúng đan vào nhau:
    /// request này SADD xong, request kia đọc SCARD rồi rollback, và có request lọt qua dù đã hết lượt.
    /// Script chạy nguyên khối trên Redis nên không có khe nào để đan.
    ///
    /// Trả {allowed, used}. Loài đã xem trong ngày luôn allowed và không cộng thêm.
    /// </summary>
    private const string ConsumeScript = """
        if redis.call('SISMEMBER', KEYS[1], ARGV[1]) == 1 then
          return {1, redis.call('SCARD', KEYS[1])}
        end
        local used = redis.call('SCARD', KEYS[1])
        if used >= tonumber(ARGV[2]) then
          return {0, used}
        end
        redis.call('SADD', KEYS[1], ARGV[1])
        redis.call('EXPIRE', KEYS[1], tonumber(ARGV[3]))
        return {1, used + 1}
        """;

    private readonly AnonQuotaSettings _settings = options.Value;

    public async Task<AnonQuotaResult> TryConsumeAsync(
        string visitorId, string clientIp, int specCode, CancellationToken ct = default)
    {
        var limit = _settings.DailySpeciesLimit;
        var resetsIn = SecondsUntilReset();

        // Hạn mức tắt, hoặc Redis chưa cấu hình (local dev) → không chặn ai cả.
        if (limit <= 0 || redis is null || !redis.IsConnected)
            return new AnonQuotaResult(true, 0, limit, resetsIn);

        try
        {
            var db = redis.GetDatabase();
            var day = LocalNow().ToString("yyyyMMdd");

            // Trần IP kiểm tra trước hạn mức khách: nếu IP đã bị chặn thì đừng trừ lượt của
            // người dùng — họ không làm gì sai, chỉ đang ngồi sau cùng một NAT với ai đó.
            if (_settings.DailyIpSpeciesLimit > 0)
            {
                var ipHash = HashIp(clientIp);
                var (ipAllowed, _) = await ConsumeAsync(db, $"fd:anon:ip:{ipHash}:{day}", specCode, _settings.DailyIpSpeciesLimit);
                if (!ipAllowed)
                {
                    logger.LogWarning("Anon IP ceiling hit for {IpHash} on day {Day}", ipHash, day);
                    return new AnonQuotaResult(false, limit, limit, resetsIn);
                }
            }

            var (allowed, used) = await ConsumeAsync(db, $"fd:anon:v:{visitorId}:{day}", specCode, limit);
            return new AnonQuotaResult(allowed, allowed ? used : limit, limit, resetsIn);
        }
        catch (Exception ex) when (ex is RedisException or TimeoutException)
        {
            // Fail-open: Redis chập chờn thì cho khách xem, không dựng tường.
            logger.LogWarning(ex, "Anon quota check failed, allowing request");
            return new AnonQuotaResult(true, 0, limit, resetsIn);
        }
    }

    private static async Task<(bool Allowed, int Used)> ConsumeAsync(IDatabase db, string key, int specCode, int limit)
    {
        var result = await db.ScriptEvaluateAsync(
            ConsumeScript,
            [key],
            [specCode, limit, KeyTtlSeconds]);

        var pair = (long[])result!;
        return (pair[0] == 1, (int)pair[1]);
    }

    private DateTime LocalNow() => DateTime.UtcNow.AddHours(_settings.ResetOffsetHours);

    private int SecondsUntilReset()
    {
        var now = LocalNow();
        return (int)(now.Date.AddDays(1) - now).TotalSeconds;
    }

    /// <summary>Băm IP trước khi làm key — log và Redis không cần giữ IP thô của người xem.</summary>
    private static string HashIp(string ip)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(ip));
        return Convert.ToHexString(bytes, 0, 8);
    }
}
