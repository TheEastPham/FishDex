using AquaHome.Common.Enums;
using AquaHome.Domain.Contests;
using AquaHome.Domain.Settings;
using AquaHome.Tests.Infrastructure;
using Microsoft.Extensions.Options;

namespace AquaHome.Tests;

public sealed class YouTubeUploadAllowanceTests
{
    private const int Type = (int)QuotaType.ContestYouTubeUploadPerDay;
    private static readonly Guid User = Guid.NewGuid();

    private readonly CountingQuotaRepo _quota = new();
    private readonly FixedClock _clock = new(new DateTimeOffset(2026, 11, 26, 20, 0, 0, TimeSpan.Zero));

    private YouTubeUploadAllowance Allowance(int dailyLimit = 5)
        => new(_quota, Options.Create(new YouTubeSettings { DailyUploadLimit = dailyLimit }), _clock);

    [Fact]
    public void The_day_turns_over_at_midnight_Pacific_not_UTC()
    {
        var allowance = Allowance();

        _clock.Now = new DateTimeOffset(2026, 11, 27, 7, 59, 0, TimeSpan.Zero); // 23:59 PST ngày 26
        Assert.Equal(new DateOnly(2026, 11, 26), allowance.Today());

        _clock.Now = new DateTimeOffset(2026, 11, 27, 8, 0, 0, TimeSpan.Zero);  // 00:00 PST ngày 27
        Assert.Equal(new DateOnly(2026, 11, 27), allowance.Today());
    }

    [Fact]
    public async Task Reports_which_limit_is_used_up()
    {
        var allowance = Allowance(dailyLimit: 5);
        var day = allowance.Today();

        Assert.Equal(AllowanceCheck.Available, await allowance.CheckAsync(User));

        _quota.Set(User, Type, day, YouTubeUploadAllowance.PerUserPerDay);
        Assert.Equal(AllowanceCheck.UserExhausted, await allowance.CheckAsync(User));
        Assert.Equal(AllowanceCheck.Available, await allowance.CheckAsync(Guid.NewGuid()));

        _quota.Set(Guid.Empty, Type, day, 5);
        Assert.Equal(AllowanceCheck.SystemExhausted, await allowance.CheckAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Reserving_counts_against_the_user_and_the_system()
    {
        var allowance = Allowance(dailyLimit: 5);
        var day = allowance.Today();

        Assert.True(await allowance.TryReserveAsync(User));
        Assert.True(await allowance.TryReserveAsync(User));
        Assert.False(await allowance.TryReserveAsync(User)); // trần riêng 2/ngày

        Assert.Equal(2, _quota.Usage(Guid.Empty, Type, day));
    }

    [Fact]
    public async Task A_new_Pacific_day_gives_a_fresh_allowance()
    {
        var allowance = Allowance();
        _quota.Set(User, Type, allowance.Today(), YouTubeUploadAllowance.PerUserPerDay);

        _clock.Now = _clock.Now.AddDays(1);

        Assert.Equal(AllowanceCheck.Available, await allowance.CheckAsync(User));
    }
}
