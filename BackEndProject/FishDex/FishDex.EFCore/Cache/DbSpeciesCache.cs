using FishDex.EFCore.DbContexts;
using FishDex.EFCore.Entity.Cache;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FishDex.EFCore.Cache;

/// <summary>
/// Reads/writes SpeciesSnapshot from the database.
/// Population pulls raw fields from FishBase tables — no inference, nullable when absent.
/// </summary>
public class DbSpeciesCache(FishDexDbContext db, ILogger<DbSpeciesCache> logger) : ISpeciesCache
{
    public async Task<SpeciesSnapshot?> GetOrPopulateAsync(int specCode, CancellationToken ct = default)
    {
        var existing = await db.SpeciesSnapshots.FindAsync([specCode], ct);
        if (existing is not null)
            return existing;

        return await PopulateInternalAsync(specCode, ct);
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
            var snapshot = await PopulateInternalAsync(code, ct);
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

        return await PopulateInternalAsync(specCode, ct);
    }

    public async Task InvalidateAsync(int specCode, CancellationToken ct = default)
    {
        var existing = await db.SpeciesSnapshots.FindAsync([specCode], ct);
        if (existing is null) return;

        db.SpeciesSnapshots.Remove(existing);
        await db.SaveChangesAsync(ct);
    }

    // ── Internal population from FishBase tables ─────────────────────────────

    private async Task<SpeciesSnapshot?> PopulateInternalAsync(int specCode, CancellationToken ct)
    {
        // Load species
        var species = await db.Species
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SpecCode == specCode, ct);

        if (species is null)
        {
            logger.LogWarning("SpeciesSnapshot population skipped: SpecCode {SpecCode} not found", specCode);
            return null;
        }

        // Load family + genus for names
        var family = await db.Families
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == species.FamId, ct);

        var genus = species.GenusCode.HasValue
            ? await db.Genuses
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.GenusCode == species.GenusCode.Value, ct)
            : null;

        // Load preferred common name (Vietnamese preferred, else first English)
        var commonName = await db.CommonNames
            .AsNoTracking()
            .Where(c => c.SpecCode == specCode && c.IsPreferred)
            .OrderByDescending(c => c.Language == "Vietnamese")
            .ThenByDescending(c => c.Language == "English")
            .Select(c => c.ComName)
            .FirstOrDefaultAsync(ct);

        // Load stock environment (temperature, pH, dH) — use first stock
        var stock = await db.Stocks
            .AsNoTracking()
            .Include(s => s.Environment)
            .Where(s => s.SpecCode == specCode)
            .FirstOrDefaultAsync(ct);

        var env = stock?.Environment;

        // Load ecology → feeding + associations + circadian
        var ecology = await db.Ecologies
            .AsNoTracking()
            .Include(e => e.FeedingAndDiet)
            .Include(e => e.Associations)
            .Include(e => e.CircadianBehavior)
            .FirstOrDefaultAsync(e => e.SpecCode == specCode, ct);

        var feeding      = ecology?.FeedingAndDiet;
        var associations = ecology?.Associations;
        var circadian    = ecology?.CircadianBehavior;

        // ObjectKey is a computed property — must load Id+Name and compute in memory
        var maleImg = await db.SystemImages
            .AsNoTracking()
            .Where(i => i.SpecCode == specCode && i.Name == species.PicPreferredNameM)
            .Select(i => new { i.Id, i.Name })
            .FirstOrDefaultAsync(ct);

        var femaleImg = await db.SystemImages
            .AsNoTracking()
            .Where(i => i.SpecCode == specCode && i.Name == species.PicPreferredNameF)
            .Select(i => new { i.Id, i.Name })
            .FirstOrDefaultAsync(ct);

        static string? ToKey(int sc, Guid? id, string? name) =>
            id.HasValue && name is not null
                ? $"{sc}/{id}{System.IO.Path.GetExtension(name)}"
                : null;

        var maleImageKey   = ToKey(specCode, maleImg?.Id, maleImg?.Name);
        var femaleImageKey = ToKey(specCode, femaleImg?.Id, femaleImg?.Name);
        var thumbKey       = maleImageKey ?? femaleImageKey;

        var snapshot = new SpeciesSnapshot
        {
            SpecCode     = specCode,
            DataSource   = SnapshotDataSource.FishBase,
            IsVerified   = false,
            SpeciesName  = species.SpeciesName,
            FamilyName   = family?.Name,
            GenusName    = genus?.GenusName,
            CommonName   = commonName,
            WaterType    = species.WaterType,
            TempMin      = env?.TempMin,
            TempMax      = env?.TempMax,
            PhMin        = env?.PHMin,
            PhMax        = env?.PHMax,
            DhMin        = env?.DHMin,
            DhMax        = env?.DHMax,
            Length           = species.Length,
            LongevityCaptive = species.LongevityCaptive,
            DemersPelag      = species.DemersPelag,
            Schooling        = associations is null ? null : (bool?)associations.Schooling,
            Shoaling         = associations is null ? null : (bool?)associations.Shoaling,
            Solitary         = associations is null ? null : (bool?)associations.Solitary,
            FeedingType      = feeding?.FeedingType,
            FeedingPosition  = null, // not in FishBase tables — community only
            ActivityPattern  = circadian?.Circadian1,
            // Community-only fields — left null on FishBase population
            RequiresLiveFood = null,
            Aggressiveness   = null,
            FinNippingRisk   = null,
            JumpingRisk      = null,
            CareLevel        = null,
            MinTankLiters    = null,
            ThumbnailObjectKey  = thumbKey,
            MaleImageObjectKey  = maleImageKey,
            FemaleImageObjectKey = femaleImageKey,
            ContributedBy    = null,
            ReviewedBy       = null,
            RejectionReason  = null,
            PopulatedAt      = DateTime.UtcNow,
            PopulatedFrom    = SnapshotPopulatedFrom.FishBase,
        };

        db.SpeciesSnapshots.Add(snapshot);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            // Race condition: another request populated the same SpecCode concurrently
            logger.LogWarning(ex, "SpeciesSnapshot insert conflict for SpecCode {SpecCode} — fetching existing", specCode);
            db.ChangeTracker.Clear();
            return await db.SpeciesSnapshots.FindAsync([specCode], ct);
        }

        logger.LogInformation("SpeciesSnapshot populated for SpecCode {SpecCode}", specCode);
        return snapshot;
    }
}
