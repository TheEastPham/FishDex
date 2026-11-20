using AquaHome.EFCore.Data;
using AquaHome.EFCore.Entity;
using AquaHome.EFCore.Repository.Interface;
using Microsoft.EntityFrameworkCore;

namespace AquaHome.EFCore.Repository;

public class ContestRepository(AquaHomeDbContext db) : IContestRepository
{
    public Task<Contest?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.Contests.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<Contest>> GetAllAsync(CancellationToken ct = default)
        => await db.Contests.OrderByDescending(c => c.StartAt).ToListAsync(ct);

    public async Task<IReadOnlyList<Contest>> GetActiveAsync(CancellationToken ct = default)
        => await db.Contests
            .Where(c => c.Status == 1) // ContestStatus.Active
            .OrderByDescending(c => c.StartAt)
            .ToListAsync(ct);

    public async Task AddAsync(Contest contest, CancellationToken ct = default)
        => await db.Contests.AddAsync(contest, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
