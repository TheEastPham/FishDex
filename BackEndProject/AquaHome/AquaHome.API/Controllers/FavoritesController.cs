using AquaHome.Domain.DTOs;
using AquaHome.Domain.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AquaHome.API.Controllers;

[ApiController]
[Route("api/favorites")]
[Authorize]
public class FavoritesController(IFavoriteService favoriteService) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<FavoriteDto>> GetAll(CancellationToken ct)
        => favoriteService.GetMyFavoritesAsync(ct);

    [HttpGet("{specCode:int}")]
    public Task<bool> IsFavorite(int specCode, CancellationToken ct)
        => favoriteService.IsFavoriteAsync(specCode, ct);

    [HttpPost("{specCode:int}")]
    public Task<FavoriteDto> Add(int specCode, CancellationToken ct)
        => favoriteService.AddAsync(specCode, ct);

    [HttpDelete("{specCode:int}")]
    public async Task<IActionResult> Remove(int specCode, CancellationToken ct)
    {
        var removed = await favoriteService.RemoveAsync(specCode, ct);
        return removed ? NoContent() : NotFound();
    }
}
