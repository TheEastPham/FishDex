using AquaHome.EFCore.Entity;

namespace AquaHome.EFCore.Repository.Interface;

public interface IContestSponsorRepository
{
    Task<ContestSponsor?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ContestSponsor>> GetByContestAsync(Guid contestId, bool activeOnly, CancellationToken ct = default);
    Task AddAsync(ContestSponsor sponsor, CancellationToken ct = default);
    void Remove(ContestSponsor sponsor);
    Task SaveChangesAsync(CancellationToken ct = default);
}
