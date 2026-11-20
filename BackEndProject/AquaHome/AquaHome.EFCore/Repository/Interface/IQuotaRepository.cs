using AquaHome.EFCore.Entity;

namespace AquaHome.EFCore.Repository.Interface;

public interface IQuotaRepository
{
    Task<RoleQuota?> GetByRoleAsync(string role, CancellationToken ct = default);
    Task<IReadOnlyList<RoleQuota>> GetAllAsync(CancellationToken ct = default);
    Task UpdateAsync(RoleQuota quota, CancellationToken ct = default);

    Task<int> CountAquariumsAsync(Guid userId, CancellationToken ct = default);
    Task<int> CountFavoritesAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Đọc usage hiện tại của user cho 1 quota theo ngày (0 nếu chưa có).</summary>
    Task<int> GetDailyUsageAsync(Guid userId, int quotaType, DateOnly day, CancellationToken ct = default);

    /// <summary>Tăng counter atomic (INSERT ON CONFLICT), trả về giá trị SAU khi tăng.</summary>
    Task<int> IncrementDailyUsageAsync(Guid userId, int quotaType, DateOnly day, CancellationToken ct = default);
}
