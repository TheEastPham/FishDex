using FishDex.Domain.DTOs.Species;
using FishLover.Shared.Common;

namespace FishDex.Domain.SpeciesProfiles;

/// <summary>
/// Species Profile — góc nhìn đọc đã lắp sẵn của một loài, dù loài đó là FishBase hay cộng đồng.
/// Caller không cần biết ngưỡng SpecCode, bảng nào chứa dữ liệu, hay rule chọn ảnh/tên/ecology:
/// mọi thứ đó nằm sau interface này (xem <see cref="SpeciesRules"/>).
///
/// <para>Hợp đồng chung:</para>
/// <list type="bullet">
///   <item><c>language</c> nhận "vi"/"vn"/"Vietnamese", "en"/"English" hoặc tên ngôn ngữ FishBase;
///         null = English. Tên thường gọi fallback về English khi ngôn ngữ yêu cầu không có.</item>
///   <item>Chỉ đọc — không ghi DB, không cache nằm ngoài request. An toàn gọi từ endpoint ẩn danh.</item>
///   <item>Truy vấn chạy tuần tự trên DbContext scoped của request — không gọi song song
///         các method trên cùng một instance.</item>
///   <item>URL ảnh là presigned (null khi storage chưa cấu hình); key ảnh luôn có nếu loài có ảnh.</item>
/// </list>
/// </summary>
public interface ISpeciesProfile
{
    /// <summary>Hồ sơ đầy đủ. Null nếu loài không tồn tại (hoặc là loài cộng đồng chưa được duyệt).</summary>
    Task<SpeciesDetailDto?> GetDetailAsync(int specCode, string? language = null, CancellationToken ct = default);

    /// <summary>
    /// Thẻ tóm tắt cho nhiều loài một lượt — giữ thứ tự đầu vào, bỏ qua mã không tồn tại.
    /// Đây là hợp đồng mà AquaHome dùng (qua /api/public/species/summaries).
    /// </summary>
    Task<IReadOnlyList<SpeciesSummaryDto>> GetSummariesAsync(
        IEnumerable<int> specCodes, string? language = null, CancellationToken ct = default);

    /// <summary>
    /// Tìm theo tên khoa học hoặc tên thường gọi. Loài cộng đồng đã duyệt đứng trước loài FishBase
    /// và chỉ xuất hiện khi không lọc theo họ/chi; phân trang xuyên suốt cả hai nguồn.
    /// </summary>
    Task<PagedResult<SpeciesSearchResultDto>> SearchAsync(
        GetSpeciesSearchRequest request, CancellationToken ct = default);

    /// <summary>Loài liên quan: cùng chi trước, bù bằng cùng họ. <paramref name="limit"/> kẹp trong 3–6.</summary>
    Task<IReadOnlyList<SpeciesSearchResultDto>> GetRelatedAsync(
        int specCode, int limit = 6, string? language = null, CancellationToken ct = default);
}
