using FishDex.Domain.DTOs.Species;
using FishDex.EFCore.Entity.Cache;

namespace FishDex.Domain.Services.Interfaces;

public enum UpdateCommunitySpeciesOutcome
{
    Updated,
    NotFound,   // không tìm thấy, hoặc không phải chủ sở hữu
    NotPending, // đã được duyệt/từ chối — không cho sửa nữa
}

public record UpdateCommunitySpeciesResult(UpdateCommunitySpeciesOutcome Outcome, CommunitySpeciesDto? Dto = null);

public interface ICommunitySpeciesService
{
    /// <summary>
    /// Tra tên gần giống TRƯỚC khi submit, để bắt nhóm sai phổ biến nhất: loài đã biết được
    /// gửi lại dưới tên thương mại địa phương hoặc tên gõ sai.
    ///
    /// <para>Cố ý tách khỏi <see cref="SubmitAsync"/> chứ không chặn giữa luồng: submit hiện
    /// trả thẳng DTO, chặn ở đó sẽ phải đổi kiểu trả về và sửa controller của một luồng đang
    /// chạy tốt. FE gọi cái này khi user gõ tên rồi hiện "có phải bạn định nói…".</para>
    /// </summary>
    Task<IReadOnlyList<SimilarSpeciesDto>> FindSimilarAsync(
        string speciesName, CancellationToken ct = default);

    /// <summary>User submit loài mới → tạo SpeciesSnapshot Community, IsVerified=false, chờ duyệt.</summary>
    Task<CommunitySpeciesDto> SubmitAsync(SubmitCommunitySpeciesRequest request, CancellationToken ct = default);

    /// <summary>Chủ sở hữu sửa lại loài đang chờ duyệt (vd: gõ nhầm, bổ sung thông tin). Chỉ cho sửa khi chưa duyệt/từ chối.</summary>
    Task<UpdateCommunitySpeciesResult> UpdateAsync(int specCode, SubmitCommunitySpeciesRequest request, CancellationToken ct = default);

    /// <summary>Danh sách loài community do user hiện tại gửi (mọi trạng thái).</summary>
    Task<IReadOnlyList<CommunitySpeciesDto>> GetMineAsync(CancellationToken ct = default);

    /// <summary>Danh sách chờ duyệt — cho admin.</summary>
    Task<IReadOnlyList<CommunitySpeciesDto>> GetPendingAsync(CancellationToken ct = default);

    /// <summary>Admin duyệt: IsVerified=true → loài xuất hiện ở search/detail. Kind null → giữ SuggestedKind.</summary>
    Task<bool> VerifyAsync(int specCode, CommunitySpeciesKind? kind, CancellationToken ct = default);

    /// <summary>Admin từ chối: set RejectionReason, giữ IsVerified=false → không hiện ở search/detail. Đây là "soft delete" — record vẫn còn để user xem lại lý do.</summary>
    Task<bool> RejectAsync(int specCode, string reason, CancellationToken ct = default);

    /// <summary>
    /// Chủ sở hữu tự xoá hẳn (hard delete) loài mình đã gửi — chỉ khi chưa được duyệt (kể cả đã bị admin từ chối).
    /// Khác với RejectAsync (admin từ chối, giữ record) — đây là user tự dọn dẹp.
    /// </summary>
    Task<bool> DeleteAsync(int specCode, CancellationToken ct = default);

    /// <summary>
    /// Cấp presigned PUT URL để user tự upload ảnh cho loài community vừa submit (chỉ chủ sở hữu).
    /// Ghi ObjectKey vào ThumbnailObjectKey ngay — ảnh chỉ hiện public sau khi admin duyệt.
    /// </summary>
    Task<CommunityImageUploadResultDto?> RequestImageUploadAsync(
        int specCode, string fileName, string contentType, CancellationToken ct = default);
}
