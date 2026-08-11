using FishDex.EFCore.Entity.Market;

namespace FishDex.EFCore.Repository.Interface;

/// <summary>
/// Một dòng của danh sách market sau khi đã gộp dữ liệu loài. Chỉ chứa thứ lọc/sắp xếp được
/// ở SQL — tên hiển thị và ảnh lấy sau từ <c>SpeciesSnapshot</c> ở tầng service, vì
/// <c>SystemImage.ObjectKey</c> là computed property nên không select được trong SQL.
/// </summary>
public sealed record TradedSpeciesRow(
    int SpecCode,
    decimal? Length,
    // Tên bản ngữ đã duyệt thuộc ngôn ngữ của quốc gia đang xem. Null = chờ được đặt tên.
    // Trả thẳng tên chứ không trả bool: nếu chỉ trả bool rồi lấy tên từ SpeciesSnapshot thì
    // hai nguồn lệch nhau — snapshot giữ tên preferred chung, có thể là tiếng Anh hoặc đã cũ.
    string? LocalName,
    TradeStatus? TradeStatus,
    LegalStatus LegalStatus,
    string? LegalNote,
    int? Vulnerability,
    string? Dangerous);

/// <summary>
/// Điều kiện lọc và phân trang cho danh sách market. Gom vào một object thay vì rải thành
/// nhiều tham số — thêm bộ lọc mới không phải đổi chữ ký ở mọi tầng.
/// </summary>
public sealed class TradedSpeciesQuery
{
    /// <summary>C_Code của FishBase, không phải alpha-2.</summary>
    public required string CountryCode { get; init; }

    /// <summary>
    /// Các ngôn ngữ của quốc gia, dùng để tính <see cref="TradedSpeciesRow.HasLocalName"/>.
    /// Đếm theo <c>Language</c> chứ KHÔNG theo <c>CountryCode</c> — tên do cộng đồng góp
    /// hiện lưu CountryCode rỗng.
    /// </summary>
    public required string[] Languages { get; init; }

    public decimal? MinLength { get; init; }
    public decimal? MaxLength { get; init; }
    public bool? HasLocalName { get; init; }

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 24;
}

public interface ITradedSpeciesRepository
{
    /// <summary>
    /// Danh sách loài đang bán ở một quốc gia, phân trang ở SQL. Trả tuple để tầng service
    /// dựng <c>PagedResult</c> — theo đúng cách <c>SpeciesRepository.SearchWithCountAsync</c> đang làm.
    /// </summary>
    Task<(IReadOnlyList<TradedSpeciesRow> Items, int TotalCount)> QueryAsync(
        TradedSpeciesQuery query, CancellationToken ct = default);

    /// <summary>Hai con số cho dải thống kê. Chỉ COUNT, không đụng tới ảnh.</summary>
    Task<(int TotalCount, int WithLocalName)> GetStatsAsync(
        string countryCode, string[] languages, CancellationToken ct = default);

    Task<TradedSpecies?> GetAsync(string countryCode, int specCode, CancellationToken ct = default);

    /// <summary>
    /// Các quốc gia đang bán một loài — cho badge trên trang chi tiết loài.
    /// Dùng index <c>(SpecCode)</c> vốn tạo ra chính cho việc này.
    /// </summary>
    Task<IReadOnlyList<string>> GetCountriesSellingAsync(int specCode, CancellationToken ct = default);

    Task AddAsync(TradedSpecies entity, CancellationToken ct = default);

    void Remove(TradedSpecies entity);

    /// <summary>
    /// Upsert idempotent cho dữ liệu suy từ bể cá. Loài đã có thì chỉ đẩy
    /// <c>LastConfirmedAt</c>; loài chưa có thì thêm mới với <c>Origin = TankDerived</c>,
    /// <c>AddedBy = null</c>. Trả về số dòng THÊM MỚI.
    /// </summary>
    Task<int> UpsertTankDerivedAsync(
        string countryCode, IReadOnlyCollection<int> specCodes, CancellationToken ct = default);

    /// <summary>
    /// Validate SpecCode theo HAI NHÁNH: dưới 500.000 tra bảng <c>Species</c>, từ 500.000 tra
    /// <c>SpeciesSnapshot</c> community đã verified. Không dùng <c>FishBaseSpeciesIndex</c> ở đây
    /// vì bảng đó không chứa loài lai nên sẽ từ chối nhầm.
    /// </summary>
    Task<bool> SpecCodeExistsAsync(int specCode, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
