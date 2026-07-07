using AquaHome.EFCore.Data;
using AquaHome.EFCore.Entity;
using AquaHome.EFCore.Repository.Interface;
using Microsoft.EntityFrameworkCore;

namespace AquaHome.EFCore.Repository;

public class AquariumSnapshotRepository(AquaHomeDbContext db) : IAquariumSnapshotRepository
{
    public Task<AquariumSnapshot?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.AquariumSnapshots.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<AquariumSnapshot?> GetBySlugAsync(string slug, CancellationToken ct = default)
        => db.AquariumSnapshots.FirstOrDefaultAsync(s => s.Slug == slug && s.IsActive, ct);

    public async Task<IReadOnlyList<AquariumSnapshot>> GetActiveByAquariumAsync(Guid aquariumId, CancellationToken ct = default)
        => await db.AquariumSnapshots
            .Where(s => s.AquariumId == aquariumId && s.IsActive)
            .OrderBy(s => s.CreatedAt)
            .ToListAsync(ct);

    public Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default)
        => db.AquariumSnapshots.AnyAsync(s => s.Slug == slug, ct);

    public async Task<(IReadOnlyList<AquariumSnapshot> Items, int TotalCount)> GetGalleryAsync(
        int? waterType, int? style, string? contest, string sort, int page, int pageSize, CancellationToken ct = default)
    {
        // Không filter vào JSONB — mọi filter đều dùng column riêng (WaterType, Style, ContestEntryId, ContestAward, LikeCount)
        var query = db.AquariumSnapshots.Where(s => s.IsActive);

        if (waterType.HasValue) query = query.Where(s => s.WaterType == waterType.Value);
        if (style.HasValue) query = query.Where(s => s.Style == style.Value);

        query = contest switch
        {
            "any" => query.Where(s => s.ContestEntryId != null),
            "winners" => query.Where(s => s.ContestAward == 3), // ContestAward.Winner (AquaHome.Domain.Enums — EFCore không reference Domain)
            _ => query,
        };

        query = sort == "newest"
            ? query.OrderByDescending(s => s.CreatedAt)
            : query.OrderByDescending(s => s.LikeCount);

        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public async Task AddAsync(AquariumSnapshot snapshot, CancellationToken ct = default)
        => await db.AquariumSnapshots.AddAsync(snapshot, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
