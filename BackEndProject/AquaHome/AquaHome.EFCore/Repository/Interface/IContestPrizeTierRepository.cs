using AquaHome.EFCore.Entity;

namespace AquaHome.EFCore.Repository.Interface;

public interface IContestPrizeTierRepository
{
    Task<ContestPrizeTier?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ContestPrizeTier>> GetByContestAsync(Guid contestId, CancellationToken ct = default);
    Task AddAsync(ContestPrizeTier tier, CancellationToken ct = default);
    void Remove(ContestPrizeTier tier);
    Task SaveChangesAsync(CancellationToken ct = default);
}
