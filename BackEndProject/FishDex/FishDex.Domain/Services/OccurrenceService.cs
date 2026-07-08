using FishDex.Domain.DTOs.Occurrence;
using FishDex.Domain.Helpers;
using FishDex.Domain.Mappings;
using FishDex.Domain.Services.Interfaces;
using FishDex.EFCore.Entity.Occurrence;
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

    public async Task<IReadOnlyList<CountryDto>> GetCountriesAsync(int specCode, CancellationToken ct = default)
    {
        var codes = await occurrenceRepo.GetDistinctCountryCodesAsync(specCode, ct);
        return codes
            .Select(code => new CountryDto(code, CountryCodeMap.Resolve(code)))
            .ToList();
    }

    public async Task<SpeciesDistributionDto> GetDistributionAsync(int specCode, CancellationToken ct = default)
    {
        var all = await occurrenceRepo.GetAllWithCoordsAsync(specCode, ct);
        return BuildDistribution(all);
    }

    public async Task<IReadOnlyDictionary<int, SpeciesDistributionDto>> GetDistributionsBatchAsync(
        IReadOnlyList<int> specCodes, CancellationToken ct = default)
    {
        if (specCodes.Count == 0)
            return new Dictionary<int, SpeciesDistributionDto>();

        var all = await occurrenceRepo.GetAllWithCoordsAsync(specCodes, ct);

        return all
            .GroupBy(o => o.SpecCode)
            .ToDictionary(g => g.Key, g => BuildDistribution(g.ToList()));
    }

    private static SpeciesDistributionDto BuildDistribution(IReadOnlyList<Occurrence> occurrences)
    {
        var countries = occurrences
            .Where(o => o.CountryCode is not null)
            .GroupBy(o => o.CountryCode!)
            .Select(g =>
            {
                var code   = g.Key;
                var points = g.Take(200)
                    .Select(o => new OccurrencePointDto(o.LatitudeDec, o.LongitudeDec, o.Locality, o.Province))
                    .ToList();
                return new CountryDistributionDto(
                    code,
                    CountryCodeMap.Resolve(code),
                    CountryCodeMap.ResolveAlpha2(code),
                    g.Count(),
                    points
                );
            })
            .OrderByDescending(c => c.Count)
            .ToList();

        return new SpeciesDistributionDto(occurrences.Count, countries);
    }
}
