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
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await favoriteService.GetMyFavoritesAsync(ct);
        return Ok(result);
    }

    [HttpGet("{specCode:int}")]
    public async Task<IActionResult> IsFavorite(int specCode, CancellationToken ct)
    {
        var isFav = await favoriteService.IsFavoriteAsync(specCode, ct);
        return Ok(new { specCode, isFavorite = isFav });
    }

    [HttpPost("{specCode:int}")]
    public async Task<IActionResult> Add(int specCode, CancellationToken ct)
    {
        var result = await favoriteService.AddAsync(specCode, ct);
        return Ok(result);
    }

    [HttpDelete("{specCode:int}")]
    public async Task<IActionResult> Remove(int specCode, CancellationToken ct)
    {
        var removed = await favoriteService.RemoveAsync(specCode, ct);
        return removed ? NoContent() : NotFound();
    }
}
