using FishDex.Domain.DTOs.MorphData;

namespace FishDex.Domain.Services.Interfaces;

public interface IMorphDataService
{
    Task<MorphDataDto?> GetByStockCodeAsync(int stockCode, CancellationToken ct = default);
    Task<IReadOnlyList<MorphDataDto>> GetBySpecCodeAsync(int specCode, CancellationToken ct = default);
}
