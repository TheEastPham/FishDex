using AquaHome.EFCore.Entity;
using AquaHome.EFCore.Repository.Base;

namespace AquaHome.EFCore.Repository.Interface;

public interface IUserFavoriteRepository : IGenericRepository<UserFavorite>
{
    Task<IReadOnlyList<UserFavorite>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task<UserFavorite?> GetAsync(Guid userId, int specCode, CancellationToken ct = default);
}
