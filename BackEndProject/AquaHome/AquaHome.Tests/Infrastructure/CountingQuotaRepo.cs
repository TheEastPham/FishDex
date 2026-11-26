using AquaHome.EFCore.Entity;
using AquaHome.EFCore.Repository.Interface;

namespace AquaHome.Tests.Infrastructure;

/// <summary>Bộ đếm quota theo ngày trong RAM — repo thật dùng INSERT ON CONFLICT mà EF InMemory không chạy được.</summary>
internal sealed class CountingQuotaRepo : IQuotaRepository
{
    private readonly Dictionary<(Guid User, int Type, DateOnly Day), int> _usage = [];

    public int Usage(Guid userId, int quotaType, DateOnly day) => _usage.GetValueOrDefault((userId, quotaType, day));

    public void Set(Guid userId, int quotaType, DateOnly day, int value) => _usage[(userId, quotaType, day)] = value;

    public Task<int> GetDailyUsageAsync(Guid userId, int quotaType, DateOnly day, CancellationToken ct = default)
        => Task.FromResult(Usage(userId, quotaType, day));

    public Task<int> IncrementDailyUsageAsync(Guid userId, int quotaType, DateOnly day, CancellationToken ct = default)
    {
        var value = Usage(userId, quotaType, day) + 1;
        _usage[(userId, quotaType, day)] = value;
        return Task.FromResult(value);
    }

    public Task<RoleQuota?> GetByRoleAsync(string role, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<IReadOnlyList<RoleQuota>> GetAllAsync(CancellationToken ct = default) => throw new NotSupportedException();
    public Task UpdateAsync(RoleQuota quota, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<int> CountAquariumsAsync(Guid userId, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<int> CountFavoritesAsync(Guid userId, CancellationToken ct = default) => throw new NotSupportedException();
}

/// <summary>Đồng hồ đứng yên ở mốc cho trước.</summary>
internal sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;
    public override DateTimeOffset GetUtcNow() => Now;
}
