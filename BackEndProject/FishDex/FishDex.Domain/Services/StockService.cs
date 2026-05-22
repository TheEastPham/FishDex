using AutoMapper;
using FishDex.Domain.DTOs.Stocks;
using FishDex.Domain.Services.Interfaces;
using FishDex.EFCore.Repository.Interface;

namespace FishDex.Domain.Services;

public class StockService(
    IStockRepository stockRepo,
    IStockConservationRepository conservationRepo,
    IStockEnvironmentRepository environmentRepo,
    IMapper mapper) : IStockService
{
    public async Task<IReadOnlyList<StockDto>> GetBySpecCodeAsync(int specCode, CancellationToken ct = default)
    {
        var items = await stockRepo.FindAsync(s => s.SpecCode == specCode);
        return mapper.Map<List<StockDto>>(items.ToList());
    }

    public async Task<StockDto?> GetByStockCodeAsync(int stockCode, CancellationToken ct = default)
    {
        var entity = await stockRepo.GetByIdAsync(stockCode);
        return entity is null ? null : mapper.Map<StockDto>(entity);
    }

    public async Task<StockConservationDto?> GetConservationAsync(int stockCode, CancellationToken ct = default)
    {
        var entity = await conservationRepo.GetByIdAsync(stockCode);
        return entity is null ? null : mapper.Map<StockConservationDto>(entity);
    }

    public async Task<StockEnvironmentDto?> GetEnvironmentAsync(int stockCode, CancellationToken ct = default)
    {
        var entity = await environmentRepo.GetByIdAsync(stockCode);
        return entity is null ? null : mapper.Map<StockEnvironmentDto>(entity);
    }
}
