using FishDex.EFCore.Entity.Cache;

namespace FishDex.EFCore.Repository.Interface;

/// <summary>
/// Truy cập SpeciesSnapshot cho luồng community (loài lai tạo do user submit).
/// Community species = DataSource=Community, SpecCode ≥ 500000.
/// </summary>
public interface ICommunitySpeciesRepository
{
    /// <summary>Cấp SpecCode community kế tiếp (≥ 500000). MAX hiện có + 1, hoặc 500000 nếu chưa có.</summary>
    Task<int> GetNextCommunitySpecCodeAsync(CancellationToken ct = default);

    Task AddAsync(SpeciesSnapshot snapshot, CancellationToken ct = default);

    /// <summary>Gỡ tracking 1 entity (dùng khi insert lỗi do trùng SpecCode, cần cấp lại code).</summary>
    void Detach(SpeciesSnapshot snapshot);

    /// <summary>Xoá hẳn 1 loài community (user tự xoá draft chưa duyệt).</summary>
    void Remove(SpeciesSnapshot snapshot);

    Task<SpeciesSnapshot?> GetCommunityByCodeAsync(int specCode, CancellationToken ct = default);

    /// <summary>Loài do 1 user gửi (mọi trạng thái) — cho trang "loài tôi gửi".</summary>
    Task<IReadOnlyList<SpeciesSnapshot>> GetByContributorAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Loài community đang chờ duyệt: chưa verified và chưa bị reject.</summary>
    Task<IReadOnlyList<SpeciesSnapshot>> GetPendingAsync(CancellationToken ct = default);

    /// <summary>Loài community ĐÃ verified khớp query (SpeciesName/CommonName) — cho search public.
    /// query null/empty → trả tất cả verified. Set nhỏ nên không phân trang ở tầng repo.</summary>
    Task<IReadOnlyList<SpeciesSnapshot>> SearchVerifiedAsync(string? query, CancellationToken ct = default);

    /// <summary>1 loài community đã verified theo code — cho trang detail.</summary>
    Task<SpeciesSnapshot?> GetVerifiedByCodeAsync(int specCode, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
