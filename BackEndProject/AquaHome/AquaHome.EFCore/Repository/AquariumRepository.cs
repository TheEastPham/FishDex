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
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

    public async Task<Aquarium?> GetByIdAndUserAsync(Guid id, Guid userId, CancellationToken ct = default)
        => await _db.Aquariums
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, ct);
}
