using AquaHome.EFCore.Entity;

namespace AquaHome.EFCore.Repository.Interface;

public interface IContestEntryRepository
{
    Task<ContestEntry?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ContestEntry>> GetByContestAsync(Guid contestId, CancellationToken ct = default);
    Task<IReadOnlyList<ContestEntry>> GetByStatusAsync(int status, CancellationToken ct = default);
    Task<IReadOnlyList<ContestEntry>> GetByActiveContestsAsync(CancellationToken ct = default);
    Task<long> SumStagingVideoBytesAsync(CancellationToken ct = default);
    Task AddAsync(ContestEntry entry, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
