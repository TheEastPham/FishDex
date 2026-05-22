using AutoMapper;
using FishDex.Domain.DTOs.Ecosystem;
using FishDex.Domain.Services.Interfaces;
using FishDex.EFCore.Repository.Interface;

namespace FishDex.Domain.Services;

public class EcosystemService(
    IEcosystemRepository ecosystemRepo,
    IEcosystemRefRepository ecosystemRefRepo,
    IMapper mapper) : IEcosystemService
{
    public async Task<IReadOnlyList<EcosystemRefDto>> GetAllEcosystemRefsAsync(CancellationToken ct = default)
    {
        var items = await ecosystemRefRepo.FindAsync(_ => true);
        return mapper.Map<List<EcosystemRefDto>>(items.ToList());
    }

    public async Task<EcosystemRefDto?> GetEcosystemRefByCodeAsync(int eCode, CancellationToken ct = default)
    {
        var item = await ecosystemRefRepo.GetByIdAsync<int>(eCode);
        return item is null ? null : mapper.Map<EcosystemRefDto>(item);
    }

    public async Task<IReadOnlyList<EcosystemDto>> GetBySpecCodeAsync(int specCode, CancellationToken ct = default)
    {
        var items = await ecosystemRepo.FindAsync(e => e.SpecCode == specCode);
        return mapper.Map<List<EcosystemDto>>(items.ToList());
    }

    public async Task<IReadOnlyList<EcosystemDto>> GetByECodeAsync(int eCode, CancellationToken ct = default)
    {
        var items = await ecosystemRepo.FindAsync(e => e.E_CODE == eCode);
        return mapper.Map<List<EcosystemDto>>(items.ToList());
    }
}
