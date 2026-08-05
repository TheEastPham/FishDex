using FishDex.EFCore.Entity.Cache;

namespace FishDex.EFCore.Entity.Market;

/// <summary>
/// Một loài được bán ở một quốc gia. Khoá chính composite (CountryCode, SpecCode) —
/// theo đúng kiểu <c>AquariumFish</c> của AquaHome đang làm.
///
/// <para><b>Không có khoá ngoại.</b> Loài cộng đồng (SpecCode ≥ 500.000) chỉ tồn tại trong
/// <see cref="SpeciesSnapshot"/>, mà bảng đó là cache-aside populate lazy nên loài chưa ai xem
/// cũng chưa có row. FK sang Species sẽ loại loài lai, FK sang SpeciesSnapshot sẽ loại loài
/// chưa cache. Validate trong service theo hai nhánh thay vì đặt FK.</para>
///
/// <para><b>Nguồn dữ liệu chính là bể cá.</b> Bể ở quốc gia X có cá Z thì quốc gia X bán cá Z.
/// Các dòng sinh theo đường đó có <see cref="Origin"/> = <see cref="MarketOrigin.TankDerived"/>
/// và <see cref="AddedBy"/> = null — <b>tuyệt đối không lưu tham chiếu user</b>, để không ai
/// truy ngược được "cá này bán ở nước X là vì user Y có nó trong bể".</para>
/// </summary>
public class TradedSpecies
{
    /// <summary>C_Code của FishBase (vd "704" = Việt Nam), không phải alpha-2.</summary>
    public string CountryCode { get; set; } = string.Empty;

    public int SpecCode { get; set; }

    // ── Thị trường ────────────────────────────────────────────────
    /// <summary>Null cho dòng sinh từ bể cá — không ai đặt giá trị này. Chỉ admin curate.</summary>
    public TradeStatus? TradeStatus { get; set; }

    /// <summary>Mặc định Legal. Chỉ admin sửa, và phải kèm nguồn khi đặt Restricted/Banned.</summary>
    public LegalStatus LegalStatus { get; set; } = LegalStatus.Legal;
    public string? LegalNote { get; set; }
    public string? LegalSourceUrl { get; set; }

    /// <summary>
    /// Độ dễ nuôi Ở QUỐC GIA NÀY, không phải của loài. Nước máy Việt Nam độ cứng khác Đức,
    /// khí hậu Việt Nam gần như không cần sưởi — nên "dễ nuôi" phụ thuộc địa phương.
    /// Để trống ở v1, chờ người curate.
    /// </summary>
    public SnapshotCareLevel? CareLevel { get; set; }

    // ── Nguồn & kiểm duyệt ────────────────────────────────────────
    public MarketOrigin Origin { get; set; } = MarketOrigin.TankDerived;

    /// <summary>
    /// Mặc định Approved — bỏ tiền kiểm vì admin là một người mà danh sách phải lên tới
    /// vài trăm loài mỗi nước. Giữ cột để hậu kiểm, admin vẫn gỡ được dòng rác.
    /// </summary>
    public MarketStatus Status { get; set; } = MarketStatus.Approved;

    /// <summary>Null cho dòng sinh từ bể cá. Chỉ có giá trị khi admin tự thêm.</summary>
    public Guid? AddedBy { get; set; }

    public Guid? ReviewedBy { get; set; }
    public string? RejectionReason { get; set; }

    // ── Metadata ──────────────────────────────────────────────────
    public DateTime FirstSeenAt { get; set; }
    /// <summary>Cập nhật mỗi lần nguồn xác nhận lại — dùng để phát hiện dữ liệu mốc.</summary>
    public DateTime LastConfirmedAt { get; set; }
}

/// <summary>Mức phổ biến trên thị trường. Null nghĩa là chưa ai curate.</summary>
public enum TradeStatus
{
    Common     = 0,
    Occasional = 1,
    Seasonal   = 2,
    Rare       = 3,
}

/// <summary>
/// Trạng thái pháp lý theo từng quốc gia. Đặt Restricted hoặc Banned thì BẮT BUỘC có
/// <see cref="TradedSpecies.LegalSourceUrl"/> — không đoán, không suy từ bảng thiếu cột quốc gia.
/// </summary>
public enum LegalStatus
{
    Legal      = 0,
    Restricted = 1,
    Banned     = 2,
}

public enum MarketStatus
{
    Pending  = 0,
    Approved = 1,
    Rejected = 2,
}

public enum MarketOrigin
{
    /// <summary>Admin khảo sát và seed tay.</summary>
    AdminSeed = 0,
    /// <summary>Suy từ bể cá của người dùng — nguồn chính, không kèm tham chiếu user.</summary>
    TankDerived = 1,
    /// <summary>Admin thêm tay từ màn danh sách hoặc chi tiết loài.</summary>
    AdminAdded = 2,
}
