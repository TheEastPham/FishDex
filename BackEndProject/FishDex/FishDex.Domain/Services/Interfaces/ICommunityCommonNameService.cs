using FishDex.Domain.DTOs.Species;

namespace FishDex.Domain.Services.Interfaces;

public enum SubmitCommonNameOutcome
{
    Created,
    SpeciesNotFound,   // không có loài FishBase với SpecCode này
    InvalidSpecies,    // SpecCode ≥ 500000 (community/hybrid — tên sửa trên snapshot, không qua đây)
    Duplicate,         // đã có tên trùng (SpecCode + Language + ComName)
    PendingExists,     // user này đã có 1 tên khác đang chờ duyệt cho cùng loài + ngôn ngữ
}

public record SubmitCommonNameResult(SubmitCommonNameOutcome Outcome, CommunityCommonNameDto? Dto = null);

public enum UpdateCommonNameOutcome
{
    Updated,
    NotFound,    // không tìm thấy, hoặc không phải chủ sở hữu
    NotPending,  // đã được duyệt/từ chối — không cho sửa nữa
    Duplicate,   // tên mới trùng tên khác đã có cho loài
}

public record UpdateCommonNameResult(UpdateCommonNameOutcome Outcome, CommunityCommonNameDto? Dto = null);

public interface ICommunityCommonNameService
{
    Task<SubmitCommonNameResult> SubmitAsync(int specCode, SubmitCommonNameRequest request, CancellationToken ct = default);

    /// <summary>Chủ sở hữu sửa lại tên đang chờ duyệt (vd: gõ nhầm). Chỉ cho sửa khi chưa được duyệt/từ chối.</summary>
    Task<UpdateCommonNameResult> UpdateAsync(int autoCtr, string comName, CancellationToken ct = default);

    Task<IReadOnlyList<CommunityCommonNameDto>> GetMineAsync(CancellationToken ct = default);

    Task<IReadOnlyList<CommunityCommonNameDto>> GetPendingAsync(CancellationToken ct = default);

    /// <summary>Admin duyệt → tên hiện public; invalidate SpeciesSnapshot để re-flatten tên preferred.</summary>
    Task<bool> VerifyAsync(int autoCtr, CancellationToken ct = default);

    /// <summary>Duyệt hàng loạt — trả về số tên đã duyệt.</summary>
    Task<int> VerifyBatchAsync(IReadOnlyList<int> autoCtrs, CancellationToken ct = default);

    /// <summary>Admin từ chối — soft delete, giữ record kèm RejectionReason để user xem lại lý do.</summary>
    Task<bool> RejectAsync(int autoCtr, string reason, CancellationToken ct = default);

    /// <summary>Chủ sở hữu tự xoá hẳn (hard delete) tên mình đã gửi — chỉ khi chưa được duyệt (kể cả đã bị từ chối).</summary>
    Task<bool> DeleteAsync(int autoCtr, CancellationToken ct = default);
}
