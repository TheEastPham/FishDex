using FishDex.EFCore.DbContexts;
using FishDex.EFCore.Entity.Species;
using FishDex.EFCore.Repository.BaseGeneric;
using FishDex.EFCore.Repository.Interface;
using Microsoft.EntityFrameworkCore;

namespace FishDex.EFCore.Repository;

public class SpeciesRepository(FishDexDbContext context)
    : GenericRepository<Species>(context), ISpeciesRepository
{
    private readonly FishDexDbContext _db = context;

    public async Task<(IReadOnlyList<Species> Items, int TotalCount)> SearchWithCountAsync(
        string? query, Guid? famId, int? genusCode, string? language,
        int page, int pageSize, CancellationToken ct = default)
    {
        var hasQuery = !string.IsNullOrWhiteSpace(query);
        var q = _db.Species
            .Include(s => s.Genus)
            .Include(s => s.Family)
            .Include(s => s.CommonNames)
            .Where(s =>
                !hasQuery ||
                EF.Functions.ILike(s.SpeciesName, $"%{query}%") ||
                s.CommonNames.Any(c =>
                    EF.Functions.ILike(c.ComName, $"%{query}%") &&
                    (language == null || c.Language == language)));

        if (famId.HasValue)    q = q.Where(s => s.FamId == famId.Value);
        if (genusCode.HasValue) q = q.Where(s => s.GenusCode == genusCode.Value);

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderBy(s => s.SpeciesName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }
}
