using AutoMapper;
using FishDex.Domain.DTOs.MorphData;
using FishDex.Domain.Services.Interfaces;
using FishDex.EFCore.Repository.Interface;

namespace FishDex.Domain.Services;

public class MorphDataService(
    IMorphDataRepository morphRepo,
    IMapper mapper) : IMorphDataService
{
    public async Task<MorphDataDto?> GetByStockCodeAsync(int stockCode, CancellationToken ct = default)
    {
        var entity = await morphRepo.GetByIdAsync(stockCode);
        return entity is null ? null : mapper.Map<MorphDataDto>(entity);
    }

    public async Task<IReadOnlyList<MorphDataDto>> GetBySpecCodeAsync(int specCode, CancellationToken ct = default)
    {
        var items = await morphRepo.FindAsync(m => m.Speccode == specCode);
        return mapper.Map<List<MorphDataDto>>(items.ToList());
    }
}
