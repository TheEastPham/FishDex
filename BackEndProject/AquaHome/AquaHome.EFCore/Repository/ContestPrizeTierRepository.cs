using AquaHome.EFCore.Data;
using AquaHome.EFCore.Entity;
using AquaHome.EFCore.Repository.Interface;
using Microsoft.EntityFrameworkCore;

namespace AquaHome.EFCore.Repository;

public class ContestPrizeTierRepository(AquaHomeDbContext db) : IContestPrizeTierRepository
{
    public Task<ContestPrizeTier?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.ContestPrizeTiers.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IReadOnlyList<ContestPrizeTier>> GetByContestAsync(Guid contestId, CancellationToken ct = default)
        => await db.ContestPrizeTiers
            .Where(t => t.ContestId == contestId)
            .OrderBy(t => t.DisplayOrder)
            .ToListAsync(ct);

    public async Task AddAsync(ContestPrizeTier tier, CancellationToken ct = default)
        => await db.ContestPrizeTiers.AddAsync(tier, ct);

    public void Remove(ContestPrizeTier tier)
        => db.ContestPrizeTiers.Remove(tier);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
