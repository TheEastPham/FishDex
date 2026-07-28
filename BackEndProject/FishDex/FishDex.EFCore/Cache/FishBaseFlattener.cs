using FishDex.EFCore.DbContexts;
using FishDex.EFCore.Entity.Cache;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FishDex.EFCore.Cache;

/// <summary>
/// Reads raw FishBase tables and maps them to a flat SpeciesSnapshot.
/// Rule: populate a field only when real data exists in DB. Never infer or fabricate values.
/// </summary>
public class FishBaseFlattener(FishDexDbContext db, ILogger<FishBaseFlattener> logger)
{
    /// <summary>
    /// Builds a SpeciesSnapshot from FishBase tables. Returns null if SpecCode not found.
    /// Language preference: tries Vietnamese first, falls back to English, then any preferred name.
    /// </summary>
    public async Task<SpeciesSnapshot?> FlattenAsync(int specCode, string language = "English", CancellationToken ct = default)
    {
        var species = await db.Species
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SpecCode == specCode, ct);

        if (species is null)
        {
            logger.LogWarning("FishBaseFlattener: SpecCode {SpecCode} not found in Species table", specCode);
            return null;
        }

        // ── Taxonomy ──────────────────────────────────────────────────────────
        var family = await db.Families
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == species.FamId, ct);

        var genus = species.GenusCode.HasValue
            ? await db.Genuses
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.GenusCode == species.GenusCode.Value, ct)
            : null;

        // ── Common name — language preference → English → first preferred ─────
        var commonName = await db.CommonNames
            .AsNoTracking()
            .Where(c => c.SpecCode == specCode && c.IsPreferred && c.IsVerified)
            .OrderByDescending(c => c.Language == language)
            .ThenByDescending(c => c.Language == "English")
            .Select(c => c.ComName)
            .FirstOrDefaultAsync(ct);

        // ── Water conditions from first available stock ───────────────────────
        var env = await db.Stocks
            .AsNoTracking()
            .Include(s => s.Environment)
            .Where(s => s.SpecCode == specCode)
            .Select(s => s.Environment)
            .FirstOrDefaultAsync(ct);

        // ── Ecology → Feeding, Associations, Circadian ────────────────────────
        var ecology = await db.Ecologies
            .AsNoTracking()
            .Include(e => e.FeedingAndDiet)
            .Include(e => e.Associations)
            .Include(e => e.CircadianBehavior)
            .FirstOrDefaultAsync(e => e.SpecCode == specCode, ct);

        var feeding      = ecology?.FeedingAndDiet;
        var associations = ecology?.Associations;
        var circadian    = ecology?.CircadianBehavior;

        // ── Images — priority: PicPreferred → PicPreferredMale → PicPreferredFem → any ─
        // ObjectKey is a computed property, so must load Id+Name and build in memory
        var images = await db.SystemImages
            .AsNoTracking()
            .Where(i => i.SpecCode == specCode)
            .Select(i => new
            {
                i.Id,
                i.Name,
                i.PicPreferred,
                i.PicPreferredMale,
                i.PicPreferredFem,
                PreferredForMale = i.Name == species.PicPreferredNameM,
                PreferredForFem  = i.Name == species.PicPreferredNameF,
            })
            .ToListAsync(ct);

        static string BuildKey(int sc, Guid id, string name) =>
            $"{sc}/{id}{System.IO.Path.GetExtension(name)}";

        var maleImg   = images.FirstOrDefault(i => i.PreferredForMale);
        var femaleImg = images.FirstOrDefault(i => i.PreferredForFem);
        var bestImg   = images.FirstOrDefault(i => i.PicPreferred == true)
                        ?? images.FirstOrDefault(i => i.PicPreferredMale == true)
                        ?? images.FirstOrDefault(i => i.PicPreferredFem == true)
                        ?? images.FirstOrDefault();

        var maleKey   = maleImg   is not null ? BuildKey(specCode, maleImg.Id, maleImg.Name)     : null;
        var femaleKey = femaleImg is not null ? BuildKey(specCode, femaleImg.Id, femaleImg.Name) : null;
        var thumbKey  = bestImg   is not null ? BuildKey(specCode, bestImg.Id, bestImg.Name)     : null;

        return new SpeciesSnapshot
        {
            SpecCode     = specCode,
            DataSource   = SnapshotDataSource.FishBase,
            IsVerified   = false,

            SpeciesName  = species.SpeciesName,
            FamilyName   = family?.Name,
            GenusName    = genus?.GenusName,
            CommonName   = commonName,

            WaterType        = species.WaterType,
            TempMin          = env?.TempMin,
            TempMax          = env?.TempMax,
            PhMin            = env?.PHMin,
            PhMax            = env?.PHMax,
            DhMin            = env?.DHMin,
            DhMax            = env?.DHMax,
            Length           = species.Length,
            LongevityCaptive = species.LongevityCaptive,
            DemersPelag      = species.DemersPelag,

            // Behavior — from Associations table, null if no record
            Schooling = associations is null ? null : (bool?)associations.Schooling,
            Shoaling  = associations is null ? null : (bool?)associations.Shoaling,
            Solitary  = associations is null ? null : (bool?)associations.Solitary,

            // Feeding — from FeedingAndDiet + CircadianBehavior, null if no record
            FeedingType     = feeding?.FeedingType,
            FeedingPosition = null,                      // not in FishBase — community only
            ActivityPattern = circadian?.Circadian1,
            RequiresLiveFood = null,                     // not inferrable from FishBase

            // Temperament — no reliable FishBase field, community-only
            Aggressiveness = null,
            FinNippingRisk = null,
            JumpingRisk    = null,

            // Community-only fields — always null for FishBase snapshots
            CareLevel     = null,
            MinTankLiters = null,

            ThumbnailObjectKey   = thumbKey,
            MaleImageObjectKey   = maleKey,
            FemaleImageObjectKey = femaleKey,

            ContributedBy   = null,
            ReviewedBy      = null,
            RejectionReason = null,

            PopulatedAt   = DateTime.UtcNow,
            PopulatedFrom = SnapshotPopulatedFrom.FishBase,
        };
    }
}
