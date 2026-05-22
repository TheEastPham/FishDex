using FishDex.Domain.DTOs.Ecosystem;
using FishDex.Domain.Mappings;
using FishDex.Domain.Services.Interfaces;
using FishDex.EFCore.Repository.Interface;

namespace FishDex.Domain.Services;

public class EcosystemService(
    IEcosystemRepository ecosystemRepo,
    IEcosystemRefRepository ecosystemRefRepo) : IEcosystemService
{
    public async Task<IReadOnlyList<EcosystemRefDto>> GetAllEcosystemRefsAsync(CancellationToken ct = default)
    {
        var items = await ecosystemRefRepo.FindAsync(_ => true);
        return items.Select(e => e.ToDto()).ToList();
    }

    public async Task<EcosystemRefDto?> GetEcosystemRefByCodeAsync(int eCode, CancellationToken ct = default)
    {
        var item = await ecosystemRefRepo.GetByIdAsync<int>(eCode);
        return item?.ToDto();
    }

    public async Task<IReadOnlyList<EcosystemDto>> GetBySpecCodeAsync(int specCode, CancellationToken ct = default)
    {
        var items = await ecosystemRepo.FindAsync(e => e.SpecCode == specCode);
        return items.Select(e => e.ToDto()).ToList();
    }

    public async Task<IReadOnlyList<EcosystemDto>> GetByECodeAsync(int eCode, CancellationToken ct = default)
    {
        var items = await ecosystemRepo.FindAsync(e => e.E_CODE == eCode);
        return items.Select(e => e.ToDto()).ToList();
    }
}
