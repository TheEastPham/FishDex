using AquaHome.Common.Enums;
using AquaHome.Domain.Settings;
using AquaHome.EFCore.Repository.Interface;
using Microsoft.Extensions.Options;

namespace AquaHome.Domain.Contests;

/// <summary>
/// Lượt đăng video dự thi lên YouTube trong ngày: trần chung <see cref="YouTubeSettings.DailyUploadLimit"/> và trần
/// riêng <see cref="PerUserPerDay"/> mỗi người. "Ngày" theo giờ Pacific vì quota YouTube Data API reset lúc nửa đêm
/// giờ đó. Lượt đã giữ không trả lại kể cả khi đăng hỏng — YouTube có thể đã trừ quota.
/// </summary>
public sealed class YouTubeUploadAllowance(
    IQuotaRepository quotaRepo,
    IOptions<YouTubeSettings> settings,
    TimeProvider clock)
{
    /// <summary>Một người chiếm tối đa chừng này lượt mỗi ngày, để vài người nộp nhiều bể không lấy hết lượt của cả hệ thống.</summary>
    public const int PerUserPerDay = 2;

    private const int Type = (int)QuotaType.ContestYouTubeUploadPerDay;

    private static readonly TimeZoneInfo PacificTime = ResolvePacificTimeZone();

    /// <summary>Hôm nay (giờ Pacific) còn lượt cho người này không. Hết lượt chung thì báo trước, vì khi đó cả hàng chờ phải đợi.</summary>
    public async Task<AllowanceCheck> CheckAsync(Guid userId, CancellationToken ct = default)
    {
        var day = Today();
        if (await quotaRepo.GetDailyUsageAsync(Guid.Empty, Type, day, ct) >= settings.Value.DailyUploadLimit)
            return AllowanceCheck.SystemExhausted;

        return await quotaRepo.GetDailyUsageAsync(userId, Type, day, ct) >= PerUserPerDay
            ? AllowanceCheck.UserExhausted
            : AllowanceCheck.Available;
    }

    /// <summary>
    /// Giữ 1 lượt (atomic) cho người này rồi cho cả hệ thống, ngay TRƯỚC khi gọi YouTube. False nếu một trong hai
    /// đã hết — lượt riêng vừa tăng không được trả lại, nên gọi <see cref="CheckAsync"/> trước để hiếm khi rơi vào đây.
    /// </summary>
    public async Task<bool> TryReserveAsync(Guid userId, CancellationToken ct = default)
    {
        var day = Today();
        return await quotaRepo.IncrementDailyUsageAsync(userId, Type, day, ct) <= PerUserPerDay
            && await quotaRepo.IncrementDailyUsageAsync(Guid.Empty, Type, day, ct) <= settings.Value.DailyUploadLimit;
    }

    /// <summary>Ngày của bộ đếm lượt, theo giờ Pacific.</summary>
    public DateOnly Today()
        => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(clock.GetUtcNow().UtcDateTime, PacificTime));

    private static TimeZoneInfo ResolvePacificTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Los_Angeles");
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            // Image thiếu tzdata → PST cố định. Mùa hè (PDT) mốc ngày lệch Google 1 tiếng; có lọt quá trần
            // thì YouTube báo hết quota và bài vẫn ở hàng chờ.
            return TimeZoneInfo.CreateCustomTimeZone("PST", TimeSpan.FromHours(-8), "PST", "PST");
        }
    }
}

public enum AllowanceCheck
{
    Available,

    /// <summary>Người này đã dùng hết lượt riêng hôm nay; bài của người khác vẫn đăng được.</summary>
    UserExhausted,

    /// <summary>Cả hệ thống hết lượt hôm nay; mọi bài đợi sang ngày sau.</summary>
    SystemExhausted,
}
