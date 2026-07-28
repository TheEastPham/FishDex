using AquaHome.EFCore.Data;
using AquaHome.EFCore.Entity;
using AquaHome.EFCore.Repository.Interface;
using Microsoft.EntityFrameworkCore;

namespace AquaHome.EFCore.Repository;

public class ContestSponsorRepository(AquaHomeDbContext db) : IContestSponsorRepository
{
    public Task<ContestSponsor?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.ContestSponsors.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<ContestSponsor>> GetByContestAsync(Guid contestId, bool activeOnly, CancellationToken ct = default)
    {
        var query = db.ContestSponsors.Where(s => s.ContestId == contestId);
        if (activeOnly) query = query.Where(s => s.IsActive);

        return await query
            .OrderBy(s => s.SponsorTier)   // Platinum trước, Partner sau
            .ThenBy(s => s.DisplayOrder)
            .ToListAsync(ct);
    }

    public async Task AddAsync(ContestSponsor sponsor, CancellationToken ct = default)
        => await db.ContestSponsors.AddAsync(sponsor, ct);

    public void Remove(ContestSponsor sponsor)
        => db.ContestSponsors.Remove(sponsor);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
