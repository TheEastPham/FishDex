using FishDex.Domain.DTOs.Occurrence;

namespace FishDex.Domain.Services.Interfaces;

public interface IOccurrenceService
{
    Task<IReadOnlyList<OccurrenceDto>> GetBySpecCodeAsync(int specCode, CancellationToken ct = default);
}
