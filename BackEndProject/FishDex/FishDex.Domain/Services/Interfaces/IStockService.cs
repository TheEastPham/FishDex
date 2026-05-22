using FishDex.Domain.DTOs.Stocks;

namespace FishDex.Domain.Services.Interfaces;

public interface IStockService
{
    Task<IReadOnlyList<StockDto>> GetBySpecCodeAsync(int specCode, CancellationToken ct = default);
    Task<StockDto?> GetByStockCodeAsync(int stockCode, CancellationToken ct = default);
    Task<StockConservationDto?> GetConservationAsync(int stockCode, CancellationToken ct = default);
    Task<StockEnvironmentDto?> GetEnvironmentAsync(int stockCode, CancellationToken ct = default);
}
