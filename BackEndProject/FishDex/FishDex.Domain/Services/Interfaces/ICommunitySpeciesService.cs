using FishDex.Domain.DTOs.Species;
using FishDex.EFCore.Entity.Cache;

namespace FishDex.Domain.Services.Interfaces;

public interface ICommunitySpeciesService
{
    /// <summary>User submit loài mới → tạo SpeciesSnapshot Community, IsVerified=false, chờ duyệt.</summary>
    Task<CommunitySpeciesDto> SubmitAsync(SubmitCommunitySpeciesRequest request, CancellationToken ct = default);

    /// <summary>Danh sách loài community do user hiện tại gửi (mọi trạng thái).</summary>
    Task<IReadOnlyList<CommunitySpeciesDto>> GetMineAsync(CancellationToken ct = default);

    /// <summary>Danh sách chờ duyệt — cho admin.</summary>
    Task<IReadOnlyList<CommunitySpeciesDto>> GetPendingAsync(CancellationToken ct = default);

    /// <summary>Admin duyệt: IsVerified=true → loài xuất hiện ở search/detail. Kind null → giữ SuggestedKind.</summary>
    Task<bool> VerifyAsync(int specCode, CommunitySpeciesKind? kind, CancellationToken ct = default);

    /// <summary>Admin từ chối: set RejectionReason, giữ IsVerified=false → không hiện ở search/detail.</summary>
    Task<bool> RejectAsync(int specCode, string reason, CancellationToken ct = default);

    /// <summary>
    /// Cấp presigned PUT URL để user tự upload ảnh cho loài community vừa submit (chỉ chủ sở hữu).
    /// Ghi ObjectKey vào ThumbnailObjectKey ngay — ảnh chỉ hiện public sau khi admin duyệt.
    /// </summary>
    Task<CommunityImageUploadResultDto?> RequestImageUploadAsync(
        int specCode, string fileName, string contentType, CancellationToken ct = default);
}
