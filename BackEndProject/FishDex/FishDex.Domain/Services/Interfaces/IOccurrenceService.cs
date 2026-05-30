using FishDex.Domain.DTOs.Occurrence;

namespace FishDex.Domain.Services.Interfaces;

public interface IOccurrenceService
{
    Task<IReadOnlyList<OccurrenceDto>> GetBySpecCodeAsync(int specCode, int limit = 500, CancellationToken ct = default);
    Task<IReadOnlyList<CountryDto>> GetCountriesAsync(int specCode, CancellationToken ct = default);
}
