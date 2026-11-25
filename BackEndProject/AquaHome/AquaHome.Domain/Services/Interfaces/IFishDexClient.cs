using AquaHome.Domain.DTOs;
using AquaHome.Domain.Exceptions;

namespace AquaHome.Domain.Services.Interfaces;

/// <remarks>
/// Hợp đồng lỗi: rỗng nghĩa là FishDex trả lời nhưng không có loài nào khớp. FishDex không trả lời được
/// (mạng/timeout/non-2xx/body hỏng) thì ném <see cref="FishDexUnavailableException"/>.
/// </remarks>
public interface IFishDexClient
{
    Task<IReadOnlyList<SpeciesSummaryDto>> GetSpeciesSummariesAsync(IReadOnlyList<int> specCodes, CancellationToken ct = default);

    /// <summary>Batch: distribution points cho nhiều loài trong 1 request. Key = specCode.</summary>
    Task<IReadOnlyDictionary<int, IReadOnlyList<DistributionPointDto>>> GetDistributionsAsync(IReadOnlyList<int> specCodes, CancellationToken ct = default);
}
