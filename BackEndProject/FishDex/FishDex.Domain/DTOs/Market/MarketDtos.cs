using FishDex.EFCore.Entity.Market;

namespace FishDex.Domain.DTOs.Market;

// ── Quốc gia ──────────────────────────────────────────────────────
/// <summary>
/// Tên nước KHÔNG dịch ở đây — FE dịch qua i18n key theo <paramref name="Alpha2"/>.
///
/// <para>Trả về CẢ nước chưa bật, kèm <paramref name="IsEnabled"/>, để dropdown hiện đủ danh sách
/// hệ thống hỗ trợ. Nước chưa bật thì FE không gọi endpoint dữ liệu mà hiện thẳng thông báo
/// chưa khảo sát — cho người dùng thấy lộ trình thay vì tưởng chỉ có mỗi một nước.</para>
/// </summary>
public record MarketCountryDto(string Alpha2, string NameEn, IReadOnlyList<string> Languages, bool IsEnabled);

// ── Bộ lọc ────────────────────────────────────────────────────────
/// <summary>
/// Khoảng kích thước khi trưởng thành, chia theo cách người nuôi nghĩ chứ không theo số liệu thô.
/// Lấy từ <c>Species.Length</c> — cột duy nhất trong nhóm sinh học đạt độ phủ 100%.
/// </summary>
public enum SizeBand
{
    Under5     = 0,
    From5To10  = 1,
    From10To20 = 2,
    Over20     = 3,
}

/// <summary>Lọc theo việc loài đã có tên bản ngữ hay chưa — vừa là bộ lọc vừa là công cụ cho người đóng góp.</summary>
public enum NameStatusFilter
{
    All     = 0,
    Has     = 1,
    Missing = 2,
}

public class MarketSpeciesQuery
{
    public int Page { get; init; } = 1;
    /// <summary>24 dòng mỗi trang. Không tải hết vì presigned URL của ảnh có hạn dùng.</summary>
    public int PageSize { get; init; } = 24;
    public SizeBand? SizeBand { get; init; }
    public NameStatusFilter NameStatus { get; init; } = NameStatusFilter.All;
}

// ── Kết quả ───────────────────────────────────────────────────────
public record MarketSpeciesDto(
    int SpecCode,
    string ScientificName,
    // LocalName: tên bản ngữ ưu tiên. Null nghĩa là loài đang chờ được đặt tên.
    string? LocalName,
    string? ImageUrl,
    decimal? LengthCm,
    TradeStatus? TradeStatus,
    LegalStatus LegalStatus,
    string? LegalNote,
    // Vulnerability: chỉ số của FishBase, phủ 100%. FE tự quyết ngưỡng hiện badge.
    int? Vulnerability,
    // Dangerous: giá trị thô của FishBase — harmless, venomous, traumatogenic… FE tự map.
    string? Dangerous);

/// <summary>
/// Ba con số của dải thống kê. <paramref name="AwaitingName"/> là con số được nhấn trên UI —
/// tiếng Việt trong FishBase gần như trống nên nó sẽ lớn, và đọc thành lời mời thay vì lời thú nhận.
/// </summary>
public record MarketStatsDto(int Traded, int WithLocalName, int AwaitingName);

// ── Admin ─────────────────────────────────────────────────────────
public record AddTradedSpeciesRequest(
    int SpecCode,
    TradeStatus? TradeStatus = null,
    LegalStatus LegalStatus = LegalStatus.Legal,
    string? LegalNote = null,
    string? LegalSourceUrl = null);

public record UpdateTradedSpeciesRequest(
    TradeStatus? TradeStatus,
    LegalStatus LegalStatus,
    string? LegalNote,
    string? LegalSourceUrl);

// ── Ingest từ AquaHome ────────────────────────────────────────────
/// <summary>
/// AquaHome báo các loài có trong bể ở một quốc gia. <b>Không kèm bất kỳ thông tin user nào</b> —
/// quan hệ chỉ đi một chiều, không truy ngược được từ danh sách market về chủ bể.
/// </summary>
public record IngestTankSpeciesRequest(string CountryAlpha2, IReadOnlyList<int> SpecCodes);

public record IngestResultDto(int Received, int Added);

// ── Tra cứu loài để phân luồng UC1/UC2/UC3 ────────────────────────
public enum SpeciesLookupOutcome
{
    /// <summary>Có trong FishDex — thêm thẳng vào danh sách quốc gia.</summary>
    InFishDex = 0,
    /// <summary>Có trong FishBase nhưng chưa nạp — cần chạy ETL, đã có SpecCode chính xác.</summary>
    NeedsMigration = 1,
}

public record SpeciesLookupDto(
    int SpecCode,
    string ScientificName,
    string? Genus,
    SpeciesLookupOutcome Outcome);
