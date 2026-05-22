using FishDex.Domain.DTOs.MorphData;
using FishDex.Domain.Mappings;
using FishDex.Domain.Services.Interfaces;
using FishDex.EFCore.Repository.Interface;

namespace FishDex.Domain.Services;

public class MorphDataService(
    IMorphDataRepository morphRepo) : IMorphDataService
{
    public async Task<MorphDataDto?> GetByStockCodeAsync(int stockCode, CancellationToken ct = default)
    {
        var entity = await morphRepo.GetByIdAsync(stockCode);
        return entity?.ToDto();
    }

    public async Task<IReadOnlyList<MorphDataDto>> GetBySpecCodeAsync(int specCode, CancellationToken ct = default)
    {
        var items = await morphRepo.FindAsync(m => m.Speccode == specCode);
        return items.Select(m => m.ToDto()).ToList();
    }
}
