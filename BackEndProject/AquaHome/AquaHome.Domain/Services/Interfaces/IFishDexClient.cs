using AquaHome.Domain.DTOs;

namespace AquaHome.Domain.Services.Interfaces;

public interface IFishDexClient
{
    Task<IReadOnlyList<SpeciesSummaryDto>> GetSpeciesSummariesAsync(IReadOnlyList<int> specCodes, CancellationToken ct = default);

    /// <summary>Batch: distribution points cho nhiều loài trong 1 request. Key = specCode.</summary>
    Task<IReadOnlyDictionary<int, IReadOnlyList<DistributionPointDto>>> GetDistributionsAsync(IReadOnlyList<int> specCodes, CancellationToken ct = default);
}
