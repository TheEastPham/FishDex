using FishDex.Domain.DTOs.Species;

namespace FishDex.Domain.Services.Interfaces;

public enum SubmitCommonNameOutcome
{
    Created,
    SpeciesNotFound,   // không có loài FishBase với SpecCode này
    InvalidSpecies,    // SpecCode ≥ 500000 (community/hybrid — tên sửa trên snapshot, không qua đây)
    Duplicate,         // đã có tên trùng (SpecCode + Language + ComName)
}

public record SubmitCommonNameResult(SubmitCommonNameOutcome Outcome, CommunityCommonNameDto? Dto = null);

public interface ICommunityCommonNameService
{
    Task<SubmitCommonNameResult> SubmitAsync(int specCode, SubmitCommonNameRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<CommunityCommonNameDto>> GetMineAsync(CancellationToken ct = default);

    Task<IReadOnlyList<CommunityCommonNameDto>> GetPendingAsync(CancellationToken ct = default);

    /// <summary>Admin duyệt → tên hiện public; invalidate SpeciesSnapshot để re-flatten tên preferred.</summary>
    Task<bool> VerifyAsync(int autoCtr, CancellationToken ct = default);

    /// <summary>Duyệt hàng loạt — trả về số tên đã duyệt.</summary>
    Task<int> VerifyBatchAsync(IReadOnlyList<int> autoCtrs, CancellationToken ct = default);

    Task<bool> RejectAsync(int autoCtr, string reason, CancellationToken ct = default);
}
