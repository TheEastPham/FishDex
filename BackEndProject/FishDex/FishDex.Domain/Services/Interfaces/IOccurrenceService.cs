using FishDex.Domain.DTOs.Occurrence;

namespace FishDex.Domain.Services.Interfaces;

public interface IOccurrenceService
{
    Task<IReadOnlyList<OccurrenceDto>> GetBySpecCodeAsync(int specCode, int limit = 500, CancellationToken ct = default);
    Task<IReadOnlyList<CountryDto>> GetCountriesAsync(int specCode, CancellationToken ct = default);
    Task<SpeciesDistributionDto> GetDistributionAsync(int specCode, CancellationToken ct = default);

    /// <summary>Batch: distribution cho nhiều loài trong 1 query. Key = specCode.</summary>
    Task<IReadOnlyDictionary<int, SpeciesDistributionDto>> GetDistributionsBatchAsync(IReadOnlyList<int> specCodes, CancellationToken ct = default);
}
