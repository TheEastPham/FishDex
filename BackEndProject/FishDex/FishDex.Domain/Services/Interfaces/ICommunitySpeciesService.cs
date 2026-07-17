using FishDex.Domain.DTOs.Species;

namespace FishDex.Domain.Services.Interfaces;

public interface ICommunitySpeciesService
{
    /// <summary>User submit loài lai tạo → tạo SpeciesSnapshot Community, IsVerified=false, chờ duyệt.</summary>
    Task<CommunitySpeciesDto> SubmitAsync(SubmitCommunitySpeciesRequest request, CancellationToken ct = default);

    /// <summary>Danh sách loài community do user hiện tại gửi (mọi trạng thái).</summary>
    Task<IReadOnlyList<CommunitySpeciesDto>> GetMineAsync(CancellationToken ct = default);

    /// <summary>Danh sách chờ duyệt — cho admin.</summary>
    Task<IReadOnlyList<CommunitySpeciesDto>> GetPendingAsync(CancellationToken ct = default);

    /// <summary>Admin duyệt: IsVerified=true → loài xuất hiện ở search/detail.</summary>
    Task<bool> VerifyAsync(int specCode, CancellationToken ct = default);

    /// <summary>Admin từ chối: set RejectionReason, giữ IsVerified=false → không hiện ở search/detail.</summary>
    Task<bool> RejectAsync(int specCode, string reason, CancellationToken ct = default);
}
