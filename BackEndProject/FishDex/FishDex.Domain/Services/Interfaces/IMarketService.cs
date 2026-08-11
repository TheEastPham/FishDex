using FishDex.Domain.DTOs.Market;
using FishLover.Shared.Common;

namespace FishDex.Domain.Services.Interfaces;

/// <summary>
/// Lớp market: danh sách cá đang được bán ở từng quốc gia.
///
/// <para>Quy ước chung: mọi method nhận <c>alpha2</c> (mã chữ từ API/URL) và tự đổi sang
/// C_Code để query DB. Trả <c>null</c> khi quốc gia không tồn tại hoặc chưa bật market —
/// controller trả 404 cho cả hai trường hợp, không phân biệt.</para>
/// </summary>
public interface IMarketService
{
    /// <summary>Chỉ các nước đã bật, để FE đổ vào dropdown.</summary>
    IReadOnlyList<MarketCountryDto> GetCountries();

    Task<PagedResult<MarketSpeciesDto>?> GetSpeciesAsync(
        string alpha2, MarketSpeciesQuery query, CancellationToken ct = default);

    Task<MarketStatsDto?> GetStatsAsync(string alpha2, CancellationToken ct = default);

    /// <summary>
    /// Các quốc gia đang bán một loài, trả về alpha-2. Cho badge trên trang chi tiết loài.
    /// Chỉ trả nước đã bật market — nước tắt thì người dùng không xem được danh sách của nó,
    /// nên hiện badge cũng vô nghĩa.
    /// </summary>
    Task<IReadOnlyList<string>> GetSellingCountriesAsync(int specCode, CancellationToken ct = default);

    /// <summary>
    /// Tra tên khoa học trên index toàn bộ FishBase để phân luồng UC1/UC2.
    /// Không trả loài lai — loài không có trong FishBase thì đi luồng community species.
    /// </summary>
    Task<IReadOnlyList<SpeciesLookupDto>> LookupAsync(
        string query, int limit = 20, CancellationToken ct = default);

    // ── Admin ─────────────────────────────────────────────────────
    Task<MarketMutationOutcome> AddAsync(
        string alpha2, AddTradedSpeciesRequest request, CancellationToken ct = default);

    Task<MarketMutationOutcome> UpdateAsync(
        string alpha2, int specCode, UpdateTradedSpeciesRequest request, CancellationToken ct = default);

    Task<MarketMutationOutcome> RemoveAsync(
        string alpha2, int specCode, CancellationToken ct = default);

    // ── Ingest từ AquaHome ────────────────────────────────────────
    Task<IngestResultDto?> IngestAsync(
        IngestTankSpeciesRequest request, CancellationToken ct = default);
}

public enum MarketMutationOutcome
{
    Ok = 0,
    /// <summary>Quốc gia không tồn tại hoặc chưa bật market.</summary>
    CountryNotFound = 1,
    /// <summary>SpecCode không có trong FishDex (cả bảng Species lẫn community đã verified).</summary>
    SpeciesNotFound = 2,
    /// <summary>Loài đã có trong danh sách của nước này.</summary>
    AlreadyExists = 3,
    /// <summary>Dòng cần sửa/xoá không tồn tại.</summary>
    NotFound = 4,
    /// <summary>Đặt Restricted hoặc Banned mà không kèm nguồn.</summary>
    LegalSourceRequired = 5,
}
