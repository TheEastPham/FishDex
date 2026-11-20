using FishDex.EFCore.Entity.Cache;

namespace FishDex.EFCore.Repository.Interface;

/// <summary>Nguồn mà một tên gần giống được tìm thấy — quyết định FE dẫn người dùng đi đâu.</summary>
public enum SimilarNameSource
{
    /// <summary>Đã có trong FishDex — chọn thẳng loài đó, không cần submit.</summary>
    FishDex = 0,
    /// <summary>Có trong FishBase nhưng chưa nạp — đi luồng yêu cầu migration (UC2).</summary>
    FishBase = 1,
    /// <summary>Đã có người khác submit — tránh trùng hàng đợi duyệt.</summary>
    Community = 2,
}

/// <param name="Score">Độ giống trigram 0..1. Càng gần 1 càng nhiều khả năng là cùng một loài.</param>
public sealed record SimilarSpeciesName(
    int SpecCode,
    string SpeciesName,
    SimilarNameSource Source,
    double Score);

/// <summary>
/// Truy cập SpeciesSnapshot cho luồng community (loài lai tạo do user submit).
/// Community species = DataSource=Community, SpecCode ≥ 500000.
/// </summary>
public interface ICommunitySpeciesRepository
{
    /// <summary>Cấp SpecCode community kế tiếp (≥ 500000). MAX hiện có + 1, hoặc 500000 nếu chưa có.</summary>
    /// <summary>
    /// Tìm tên khoa học gần giống trên cả ba nguồn: loài FishBase đã nạp, index toàn bộ
    /// FishBase, và loài cộng đồng đã submit. Dùng pg_trgm.
    ///
    /// <para>Mục đích là chặn từ gốc nhóm submission sai phổ biến nhất — loài đã biết được
    /// gửi lại dưới tên thương mại địa phương hoặc tên gõ sai. Rẻ hơn và đáng tin hơn nhiều
    /// so với tra mạng, và không có rủi ro bịa dữ liệu.</para>
    /// </summary>
    Task<IReadOnlyList<SimilarSpeciesName>> FindSimilarNamesAsync(
        string speciesName, double threshold = 0.4, int limit = 5, CancellationToken ct = default);

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
