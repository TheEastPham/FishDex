using FishDex.EFCore.DbContexts;
using FishDex.EFCore.Entity.Species;
using FishDex.EFCore.Repository.Interface;
using Microsoft.EntityFrameworkCore;

namespace FishDex.EFCore.Repository;

public class CommunityCommonNameRepository(FishDexDbContext db) : ICommunityCommonNameRepository
{
    public async Task<bool> SpeciesExistsAsync(int specCode, CancellationToken ct = default)
        => await db.Species.AnyAsync(s => s.SpecCode == specCode, ct);

    public async Task<bool> ExistsAsync(int specCode, string comName, string? language, CancellationToken ct = default)
    {
        var name = comName.Trim();
        return await db.CommonNames.AnyAsync(
            c => c.SpecCode == specCode
                 && c.Language == language
                 && c.ComName.ToLower() == name.ToLower(), ct);
    }

    public async Task AddAsync(CommonName name, CancellationToken ct = default)
        => await db.CommonNames.AddAsync(name, ct);

    public async Task<CommonName?> GetContributedByIdAsync(int autoCtr, CancellationToken ct = default)
        => await db.CommonNames
            .FirstOrDefaultAsync(c => c.AutoCtr == autoCtr && c.ContributedBy != null, ct);

    public async Task<IReadOnlyList<CommonName>> GetPendingAsync(CancellationToken ct = default)
        => await db.CommonNames
            .Where(c => c.ContributedBy != null && !c.IsVerified && c.RejectionReason == null)
            .OrderBy(c => c.AutoCtr)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<IReadOnlyList<CommonName>> GetByContributorAsync(Guid userId, CancellationToken ct = default)
        => await db.CommonNames
            .Where(c => c.ContributedBy == userId)
            .OrderByDescending(c => c.AutoCtr)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await db.SaveChangesAsync(ct);
}
