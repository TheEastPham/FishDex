namespace FishDex.Domain.Helpers;

/// <summary>
/// Danh sách quốc gia có lớp market (cá đang kinh doanh theo quốc gia).
///
/// KHÔNG gộp với <see cref="CountryCodeMap"/> — file đó phục vụ bản đồ phân bố tự nhiên
/// với hơn 100 nước, chỉ cần numeric → tên. File này là config của một tính năng:
/// 12 nước, kèm ngôn ngữ và cờ bật/tắt. Hai mục đích khác nhau, cố ý để riêng.
///
/// <para><b>Mã quốc gia:</b> <c>Code</c> là C_Code của FishBase và là giá trị LƯU TRONG DB
/// (khớp <c>CommonName.CountryCode</c>, <c>Occurrence.CountryCode</c>, <c>TradedSpecies.CountryCode</c>).
/// <c>Alpha2</c> chỉ dùng ở API và URL. Chuyển đổi tại biên, không lưu alpha-2 vào DB.</para>
///
/// <para><b>Vì sao curate tay thay vì sinh từ countref.parquet:</b> ISO2 không phải khoá
/// duy nhất trong FishBase — <c>GB</c> ứng với cả <c>826</c> (UK) và <c>830</c> (Channel Is.),
/// và Đài Loan mang mã <c>156A</c> chứ không phải ISO numeric thuần.</para>
///
/// <para><b>Tên ngôn ngữ</b> phải viết ĐÚNG như FishBase, lấy từ <c>languagecountry.parquet</c>
/// join <c>language.parquet</c>. Lưu ý tiếng Indonesia là <c>"Bahasa Indonesia"</c> chứ không
/// phải "Indonesian" — viết sai thì bộ đếm tên bản ngữ luôn bằng 0.</para>
/// </summary>
public static class MarketCountries
{
    /// <param name="Code">C_Code của FishBase — giá trị lưu trong DB.</param>
    /// <param name="Alpha2">ISO 3166-1 alpha-2, chữ in — dùng ở API và URL.</param>
    /// <param name="Languages">Tên ngôn ngữ đúng theo cách FishBase viết. Dùng để đếm loài đã có tên bản ngữ.</param>
    /// <param name="IsEnabled">Bật trang market cho nước này hay chưa.</param>
    public sealed record MarketCountry(
        string Code,
        string Alpha2,
        string NameEn,
        string[] Languages,
        bool IsEnabled,
        int DisplayOrder);

    // Thứ tự: Việt Nam trước (nước mặc định của sản phẩm), phần còn lại theo quy mô thị trường.
    // Chỉ Việt Nam được bật ở v1 — trang trống của các nước khác trông tệ hơn là không có trang.
    private static readonly MarketCountry[] _all =
    [
        new("704", "VN", "Viet Nam",   ["Vietnamese"],                                          IsEnabled: true,  DisplayOrder: 1),
        new("840", "US", "USA",        ["English"],                                             IsEnabled: false, DisplayOrder: 2),
        new("156", "CN", "China",      ["Mandarin Chinese"],                                    IsEnabled: false, DisplayOrder: 3),
        new("392", "JP", "Japan",      ["Japanese"],                                            IsEnabled: false, DisplayOrder: 4),
        new("528", "NL", "Netherlands",["Dutch"],                                               IsEnabled: false, DisplayOrder: 5),
        new("276", "DE", "Germany",    ["German"],                                              IsEnabled: false, DisplayOrder: 6),
        new("826", "GB", "UK",         ["English"],                                             IsEnabled: false, DisplayOrder: 7),
        new("356", "IN", "India",      ["Hindi", "English"],                                    IsEnabled: false, DisplayOrder: 8),
        new("458", "MY", "Malaysia",   ["Malay", "English"],                                    IsEnabled: false, DisplayOrder: 9),
        new("702", "SG", "Singapore",  ["English", "Malay", "Mandarin Chinese", "Tamil"],       IsEnabled: false, DisplayOrder: 10),
        new("764", "TH", "Thailand",   ["Thai"],                                                IsEnabled: false, DisplayOrder: 11),
        new("360", "ID", "Indonesia",  ["Bahasa Indonesia", "Malay"],                            IsEnabled: false, DisplayOrder: 12),
    ];

    private static readonly Dictionary<string, MarketCountry> _byCode =
        _all.ToDictionary(c => c.Code, StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, MarketCountry> _byAlpha2 =
        _all.ToDictionary(c => c.Alpha2, StringComparer.OrdinalIgnoreCase);

    /// <summary>Toàn bộ 12 nước, kể cả nước chưa bật.</summary>
    public static IReadOnlyList<MarketCountry> All { get; } =
        _all.OrderBy(c => c.DisplayOrder).ToArray();

    /// <summary>Chỉ các nước đã bật trang market — dùng cho endpoint public.</summary>
    public static IReadOnlyList<MarketCountry> Enabled { get; } =
        _all.Where(c => c.IsEnabled).OrderBy(c => c.DisplayOrder).ToArray();

    /// <summary>Tra theo C_Code (giá trị trong DB).</summary>
    public static MarketCountry? ByCode(string? code) =>
        string.IsNullOrWhiteSpace(code) ? null
            : _byCode.TryGetValue(code.Trim(), out var c) ? c : null;

    /// <summary>Tra theo alpha-2 (giá trị từ API/URL). Không phân biệt chữ hoa thường.</summary>
    public static MarketCountry? ByAlpha2(string? alpha2) =>
        string.IsNullOrWhiteSpace(alpha2) ? null
            : _byAlpha2.TryGetValue(alpha2.Trim(), out var c) ? c : null;

    /// <summary>
    /// Đổi alpha-2 từ API sang C_Code để query DB. Trả về null nếu không phải nước có market
    /// hoặc nước đó chưa được bật — controller nên trả 404 trong cả hai trường hợp.
    /// </summary>
    public static string? ResolveEnabledCode(string? alpha2)
    {
        var country = ByAlpha2(alpha2);
        return country is { IsEnabled: true } ? country.Code : null;
    }
}
