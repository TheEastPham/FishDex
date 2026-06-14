using AquaHome.EFCore.Data;
using AquaHome.EFCore.Entity;
using AquaHome.EFCore.Repository.Base;
using AquaHome.EFCore.Repository.Interface;
using Microsoft.EntityFrameworkCore;

namespace AquaHome.EFCore.Repository;

public class AquariumRepository(AquaHomeDbContext context)
    : GenericRepository<Aquarium>(context), IAquariumRepository
{
    private readonly AquaHomeDbContext _db = context;

    public async Task<IReadOnlyList<Aquarium>> GetByUserAsync(Guid userId, CancellationToken ct = default)
        => await _db.Aquariums
            .Include(a => a.Fish)
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

    public async Task<Aquarium?> GetByIdAndUserAsync(Guid id, Guid userId, CancellationToken ct = default)
        => await _db.Aquariums
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, ct);

    public async Task<AquariumFish?> GetFishEntryAsync(Guid aquariumId, int specCode, CancellationToken ct = default)
        => await _db.AquariumFish
            .FirstOrDefaultAsync(f => f.AquariumId == aquariumId && f.SpecCode == specCode, ct);

    public async Task AddFishAsync(AquariumFish fish, CancellationToken ct = default)
    {
        _db.AquariumFish.Add(fish);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveFishAsync(AquariumFish fish, CancellationToken ct = default)
    {
        _db.AquariumFish.Remove(fish);
        await _db.SaveChangesAsync(ct);
    }
}
