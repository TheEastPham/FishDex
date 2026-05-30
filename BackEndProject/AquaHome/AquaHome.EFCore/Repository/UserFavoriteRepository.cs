using AquaHome.EFCore.Data;
using AquaHome.EFCore.Entity;
using AquaHome.EFCore.Repository.Base;
using AquaHome.EFCore.Repository.Interface;
using Microsoft.EntityFrameworkCore;

namespace AquaHome.EFCore.Repository;

public class UserFavoriteRepository(AquaHomeDbContext context)
    : GenericRepository<UserFavorite>(context), IUserFavoriteRepository
{
    private readonly AquaHomeDbContext _db = context;

    public async Task<IReadOnlyList<UserFavorite>> GetByUserAsync(Guid userId, CancellationToken ct = default)
        => await _db.UserFavorites
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.AddedAt)
            .ToListAsync(ct);

    public async Task<UserFavorite?> GetAsync(Guid userId, int specCode, CancellationToken ct = default)
        => await _db.UserFavorites
            .FirstOrDefaultAsync(f => f.UserId == userId && f.SpecCode == specCode, ct);
}
