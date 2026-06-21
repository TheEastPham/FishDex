using FishDex.EFCore.DbContexts;
using FishDex.EFCore.Entity.Cache;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FishDex.EFCore.Cache;

/// <summary>
/// Reads/writes SpeciesSnapshot from the database (cache-aside).
/// Population is delegated to FishBaseFlattener — no inline mapping here.
/// </summary>
public class DbSpeciesCache(
    FishDexDbContext db,
    FishBaseFlattener flattener,
    ILogger<DbSpeciesCache> logger) : ISpeciesCache
{
    public async Task<SpeciesSnapshot?> GetOrPopulateAsync(int specCode, CancellationToken ct = default)
    {
        var existing = await db.SpeciesSnapshots.FindAsync([specCode], ct);
        if (existing is not null)
            return existing;

        return await PopulateAndSaveAsync(specCode, ct);
    }

    public async Task<IReadOnlyList<SpeciesSnapshot>> GetOrPopulateManyAsync(
        IEnumerable<int> specCodes, CancellationToken ct = default)
    {
        var codes = specCodes.ToHashSet();
        var cached = await db.SpeciesSnapshots
            .Where(s => codes.Contains(s.SpecCode))
            .ToListAsync(ct);

        var missing = codes.Except(cached.Select(s => s.SpecCode)).ToList();
        foreach (var code in missing)
        {
            var snapshot = await PopulateAndSaveAsync(code, ct);
            if (snapshot is not null)
                cached.Add(snapshot);
        }

        return cached;
    }

    public async Task<SpeciesSnapshot?> RefreshAsync(int specCode, CancellationToken ct = default)
    {
        var existing = await db.SpeciesSnapshots.FindAsync([specCode], ct);
        if (existing is not null)
        {
            db.SpeciesSnapshots.Remove(existing);
            await db.SaveChangesAsync(ct);
        }

        return await PopulateAndSaveAsync(specCode, ct);
    }

    public async Task InvalidateAsync(int specCode, CancellationToken ct = default)
    {
        var existing = await db.SpeciesSnapshots.FindAsync([specCode], ct);
        if (existing is null) return;

        db.SpeciesSnapshots.Remove(existing);
        await db.SaveChangesAsync(ct);
    }

    // ─────────────────────────────────────────────────────────────────────────

    private async Task<SpeciesSnapshot?> PopulateAndSaveAsync(int specCode, CancellationToken ct)
    {
        var snapshot = await flattener.FlattenAsync(specCode, ct: ct);
        if (snapshot is null)
            return null;

        db.SpeciesSnapshots.Add(snapshot);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            // Race condition: another request inserted the same SpecCode concurrently
            logger.LogWarning(ex, "SpeciesSnapshot insert conflict for SpecCode {SpecCode} — fetching existing", specCode);
            db.ChangeTracker.Clear();
            return await db.SpeciesSnapshots.FindAsync([specCode], ct);
        }

        logger.LogInformation("SpeciesSnapshot populated for SpecCode {SpecCode}", specCode);
        return snapshot;
    }
}
