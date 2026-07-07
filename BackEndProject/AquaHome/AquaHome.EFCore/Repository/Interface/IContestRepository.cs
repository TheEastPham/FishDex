using AquaHome.EFCore.Entity;

namespace AquaHome.EFCore.Repository.Interface;

public interface IContestRepository
{
    Task<Contest?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Contest>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Contest>> GetActiveAsync(CancellationToken ct = default);
    Task AddAsync(Contest contest, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
