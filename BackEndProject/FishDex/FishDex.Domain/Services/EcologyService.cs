using FishDex.Domain.DTOs.Ecologies;
using FishDex.Domain.Mappings;
using FishDex.Domain.Services.Interfaces;
using FishDex.EFCore.Repository.Interface;
using Microsoft.Extensions.Caching.Memory;

namespace FishDex.Domain.Services;

public class EcologyService(
    IEcologyRepository ecologyRepo,
    IFeedingAndDietRepository feedingRepo,
    IHabitatZoneRepository habitatRepo,
    IAssociationsRepository associationsRepo,
    ISubstrateRepository substrateRepo,
    ISpecialHabitatRepository specialHabitatRepo,
    IMemoryCache cache) : IEcologyService
{
    private static readonly MemoryCacheEntryOptions HabitatCacheOptions = new MemoryCacheEntryOptions()
        .SetSize(1)
        .SetSlidingExpiration(TimeSpan.FromHours(4))
        .SetAbsoluteExpiration(TimeSpan.FromHours(24));
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

    public async Task<SubstrateDto?> GetSubstrateAsync(int ecologyId, CancellationToken ct = default)
    {
        var key = $"habitat:substrate:{ecologyId}";
        if (cache.TryGetValue(key, out SubstrateDto? cached))
            return cached;

        var results = await substrateRepo.FindAsync(s => s.EcologyId == ecologyId);
        var dto = results.FirstOrDefault()?.ToDto();
        if (dto is not null)
            cache.Set(key, dto, HabitatCacheOptions);
        return dto;
    }

    public async Task<SpecialHabitatDto?> GetSpecialHabitatAsync(int ecologyId, CancellationToken ct = default)
    {
        var key = $"habitat:special:{ecologyId}";
        if (cache.TryGetValue(key, out SpecialHabitatDto? cached))
            return cached;

        var results = await specialHabitatRepo.FindAsync(h => h.EcologyId == ecologyId);
        var dto = results.FirstOrDefault()?.ToDto();
        if (dto is not null)
            cache.Set(key, dto, HabitatCacheOptions);
        return dto;
    }
}
