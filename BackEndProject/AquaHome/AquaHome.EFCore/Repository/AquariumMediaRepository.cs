using AquaHome.EFCore.Data;
using AquaHome.EFCore.Entity;
using AquaHome.EFCore.Repository.Interface;
using Microsoft.EntityFrameworkCore;

namespace AquaHome.EFCore.Repository;

public class AquariumMediaRepository(AquaHomeDbContext db) : IAquariumMediaRepository
{
    public async Task<IReadOnlyList<AquariumMedia>> GetByAquariumAsync(Guid aquariumId, CancellationToken ct = default)
        => await db.AquariumMedia
            .Where(m => m.AquariumId == aquariumId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(ct);

    public Task<int> CountByAquariumAsync(Guid aquariumId, CancellationToken ct = default)
        => db.AquariumMedia.CountAsync(m => m.AquariumId == aquariumId, ct);

    public Task<AquariumMedia?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.AquariumMedia.FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task AddAsync(AquariumMedia media, CancellationToken ct = default)
        => await db.AquariumMedia.AddAsync(media, ct);

    public void Remove(AquariumMedia media)
        => db.AquariumMedia.Remove(media);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
