using AquaHome.Domain.DTOs;
using AquaHome.Domain.Services.Interfaces;
using AquaHome.EFCore.Entity;
using AquaHome.EFCore.Repository.Interface;
using FishLover.Shared.Services;

namespace AquaHome.Domain.Services;

public class FavoriteService(
    IUserFavoriteRepository favoriteRepo,
    ICurrentUserSession currentUser) : IFavoriteService
{
    public async Task<IReadOnlyList<FavoriteDto>> GetMyFavoritesAsync(CancellationToken ct = default)
    {
        var list = await favoriteRepo.GetByUserAsync(currentUser.UserId, ct);
        return list.Select(f => new FavoriteDto(f.SpecCode, f.AddedAt)).ToList();
    }

    public async Task<bool> IsFavoriteAsync(int specCode, CancellationToken ct = default)
        => await favoriteRepo.GetAsync(currentUser.UserId, specCode, ct) is not null;

    public async Task<FavoriteDto> AddAsync(int specCode, CancellationToken ct = default)
    {
        var existing = await favoriteRepo.GetAsync(currentUser.UserId, specCode, ct);
        if (existing is not null)
            return new FavoriteDto(existing.SpecCode, existing.AddedAt);

        var entity = new UserFavorite
        {
            UserId   = currentUser.UserId,
            SpecCode = specCode,
            AddedAt  = DateTime.UtcNow
        };

        await favoriteRepo.AddAsync(entity);
        return new FavoriteDto(entity.SpecCode, entity.AddedAt);
    }

    public async Task<bool> RemoveAsync(int specCode, CancellationToken ct = default)
    {
        var entity = await favoriteRepo.GetAsync(currentUser.UserId, specCode, ct);
        if (entity is null) return false;

        await favoriteRepo.DeleteAsync(entity);
        return true;
    }
}
