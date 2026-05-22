using AutoMapper;
using FishDex.Domain.DTOs.Ecologies;
using FishDex.Domain.Services.Interfaces;
using FishDex.EFCore.Repository.Interface;

namespace FishDex.Domain.Services;

public class EcologyService(
    IEcologyRepository ecologyRepo,
    IFeedingAndDietRepository feedingRepo,
    IHabitatZoneRepository habitatRepo,
    IMapper mapper) : IEcologyService
{
    public async Task<EcologyDto?> GetBySpecCodeAsync(int specCode, CancellationToken ct = default)
    {
        var results = await ecologyRepo.FindAsync(e => e.SpecCode == specCode);
        var entity = results.FirstOrDefault();
        return entity is null ? null : mapper.Map<EcologyDto>(entity);
    }

    public async Task<FeedingAndDietDto?> GetFeedingAsync(int ecologyId, CancellationToken ct = default)
    {
        var results = await feedingRepo.FindAsync(f => f.EcologyId == ecologyId);
        var entity = results.FirstOrDefault();
        return entity is null ? null : mapper.Map<FeedingAndDietDto>(entity);
    }

    public async Task<HabitatZoneDto?> GetHabitatZoneAsync(int ecologyId, CancellationToken ct = default)
    {
        var results = await habitatRepo.FindAsync(h => h.EcologyId == ecologyId);
        var entity = results.FirstOrDefault();
        return entity is null ? null : mapper.Map<HabitatZoneDto>(entity);
    }
}
