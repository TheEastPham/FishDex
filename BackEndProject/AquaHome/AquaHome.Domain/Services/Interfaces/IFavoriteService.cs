using AquaHome.Domain.DTOs;

namespace AquaHome.Domain.Services.Interfaces;

public interface IFavoriteService
{
    Task<IReadOnlyList<FavoriteDto>> GetMyFavoritesAsync(CancellationToken ct = default);
    Task<bool> IsFavoriteAsync(int specCode, CancellationToken ct = default);
    Task<FavoriteDto> AddAsync(int specCode, CancellationToken ct = default);
    Task<bool> RemoveAsync(int specCode, CancellationToken ct = default);
}
