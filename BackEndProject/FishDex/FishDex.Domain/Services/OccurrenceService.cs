using FishDex.Domain.DTOs.Occurrence;
using FishDex.Domain.Mappings;
using FishDex.Domain.Services.Interfaces;
using FishDex.EFCore.Repository.Interface;

namespace FishDex.Domain.Services;

public class OccurrenceService(
    IOccurrenceRepository occurrenceRepo) : IOccurrenceService
{
    public async Task<IReadOnlyList<OccurrenceDto>> GetBySpecCodeAsync(int specCode, CancellationToken ct = default)
    {
        var items = await occurrenceRepo.FindAsync(o => o.SpecCode == specCode);
        return items.Select(o => o.ToDto()).ToList();
    }
}
