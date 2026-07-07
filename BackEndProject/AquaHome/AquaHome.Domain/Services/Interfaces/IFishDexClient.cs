using AquaHome.Domain.DTOs;

namespace AquaHome.Domain.Services.Interfaces;

public interface IFishDexClient
{
    Task<IReadOnlyList<SpeciesSummaryDto>> GetSpeciesSummariesAsync(IReadOnlyList<int> specCodes, CancellationToken ct = default);
    Task<IReadOnlyList<DistributionPointDto>> GetOccurrencesAsync(int specCode, CancellationToken ct = default);
}
