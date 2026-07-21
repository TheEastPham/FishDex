using FishDex.EFCore.Entity.Species;

namespace FishDex.EFCore.Repository.Interface;

/// <summary>
/// Truy cập CommonName cho luồng community (user đóng góp tên địa phương cho loài FishBase).
/// Tên community = ContributedBy != null; hiển thị công khai khi IsVerified=true.
/// </summary>
public interface ICommunityCommonNameRepository
{
    /// <summary>Loài FishBase có tồn tại không (SpecCode &lt; 500000, có row trong Species)?</summary>
    Task<bool> SpeciesExistsAsync(int specCode, CancellationToken ct = default);

    /// <summary>Đã có tên trùng (cùng SpecCode + Language + ComName, không phân biệt hoa thường)?</summary>
    Task<bool> ExistsAsync(int specCode, string comName, string? language, CancellationToken ct = default);

    /// <summary>User này đã có 1 tên khác đang chờ duyệt cho cùng SpecCode + Language chưa? (safety net — FE đã tự chặn trước qua GetMineAsync)</summary>
    Task<bool> HasPendingByUserAsync(Guid userId, int specCode, string? language, CancellationToken ct = default);

    Task AddAsync(CommonName name, CancellationToken ct = default);

    /// <summary>1 tên do user đóng góp theo PK (chỉ trả về nếu ContributedBy != null).</summary>
    Task<CommonName?> GetContributedByIdAsync(int autoCtr, CancellationToken ct = default);

    /// <summary>Nhiều tên do user đóng góp theo danh sách PK — cho duyệt hàng loạt.</summary>
    Task<IReadOnlyList<CommonName>> GetContributedByIdsAsync(IReadOnlyList<int> autoCtrs, CancellationToken ct = default);

    /// <summary>Tên community đang chờ duyệt: ContributedBy != null, chưa verified, chưa bị reject.</summary>
    Task<IReadOnlyList<CommonName>> GetPendingAsync(CancellationToken ct = default);

    /// <summary>Tên do 1 user đóng góp (mọi trạng thái).</summary>
    Task<IReadOnlyList<CommonName>> GetByContributorAsync(Guid userId, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
