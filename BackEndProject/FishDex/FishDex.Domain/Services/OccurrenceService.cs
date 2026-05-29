using FishDex.Domain.DTOs.Occurrence;
using FishDex.Domain.Mappings;
using FishDex.Domain.Services.Interfaces;
using FishDex.EFCore.Repository.Interface;

namespace FishDex.Domain.Services;

public class OccurrenceService(
    IOccurrenceRepository occurrenceRepo) : IOccurrenceService
{
    public async Task<IReadOnlyList<OccurrenceDto>> GetBySpecCodeAsync(int specCode, int limit = 500, CancellationToken ct = default)
    {
        var items = await occurrenceRepo.FindAsync(
            o => o.SpecCode == specCode
              && o.LatitudeDec  != 0
              && o.LongitudeDec != 0);
        return items.Take(limit).Select(o => o.ToDto()).ToList();
    }
}
