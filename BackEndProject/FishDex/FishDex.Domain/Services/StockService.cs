using FishDex.Domain.DTOs.Stocks;
using FishDex.Domain.Mappings;
using FishDex.Domain.Services.Interfaces;
using FishDex.EFCore.Repository.Interface;

namespace FishDex.Domain.Services;

public class StockService(
    IStockRepository stockRepo,
    IStockConservationRepository conservationRepo,
    IStockEnvironmentRepository environmentRepo) : IStockService
{
    public async Task<IReadOnlyList<StockDto>> GetBySpecCodeAsync(int specCode, CancellationToken ct = default)
    {
        var items = await stockRepo.FindAsync(s => s.SpecCode == specCode);
        return items.Select(s => s.ToDto()).ToList();
    }

    public async Task<StockDto?> GetByStockCodeAsync(int stockCode, CancellationToken ct = default)
    {
        var entity = await stockRepo.GetByIdAsync(stockCode);
        return entity?.ToDto();
    }

    public async Task<StockConservationDto?> GetConservationAsync(int stockCode, CancellationToken ct = default)
    {
        var entity = await conservationRepo.GetByIdAsync(stockCode);
        return entity?.ToDto();
    }

    public async Task<StockEnvironmentDto?> GetEnvironmentAsync(int stockCode, CancellationToken ct = default)
    {
        var entity = await environmentRepo.GetByIdAsync(stockCode);
        return entity?.ToDto();
    }
}
