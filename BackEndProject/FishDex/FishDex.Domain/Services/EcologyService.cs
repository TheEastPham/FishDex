using FishDex.Domain.DTOs.Ecologies;
using FishDex.Domain.Mappings;
using FishDex.Domain.Services.Interfaces;
using FishDex.EFCore.Repository.Interface;

namespace FishDex.Domain.Services;

public class EcologyService(
    IEcologyRepository ecologyRepo,
    IFeedingAndDietRepository feedingRepo,
    IHabitatZoneRepository habitatRepo,
    IAssociationsRepository associationsRepo) : IEcologyService
{
    public async Task<EcologyDto?> GetBySpecCodeAsync(int specCode, CancellationToken ct = default)
    {
        var results = await ecologyRepo.FindAsync(e => e.SpecCode == specCode);
        return results.FirstOrDefault()?.ToDto();
    }

    public async Task<FeedingAndDietDto?> GetFeedingAsync(int ecologyId, CancellationToken ct = default)
    {
        var results = await feedingRepo.FindAsync(f => f.EcologyId == ecologyId);
        return results.FirstOrDefault()?.ToDto();
    }

    public async Task<HabitatZoneDto?> GetHabitatZoneAsync(int ecologyId, CancellationToken ct = default)
    {
        var results = await habitatRepo.FindAsync(h => h.EcologyId == ecologyId);
        return results.FirstOrDefault()?.ToDto();
    }

    public async Task<AssociationsDto?> GetAssociationsAsync(int ecologyId, CancellationToken ct = default)
    {
        var results = await associationsRepo.FindAsync(a => a.EcologyId == ecologyId);
        var a = results.FirstOrDefault();
        if (a is null) return null;
        return new AssociationsDto
        {
            EcologyId = a.EcologyId,
            Schooling = a.Schooling,
            Shoaling  = a.Shoaling,
            Solitary  = a.Solitary
        };
    }
}
