namespace FishDex.Domain.Settings;

/// <summary>
/// Hạn mức xem profile loài cho khách chưa đăng nhập. Đơn vị đếm là <b>loài</b>, không phải request:
/// một trang profile gọi 4-5 API và cache FE chỉ sống 5 phút, nên đếm theo request thì refresh hay
/// quay lại loài cũ đều bị trừ tiếp — người dùng không hiểu nổi mình đang tiêu gì.
/// </summary>
public class AnonQuotaSettings
{
    public const string SectionName = "AnonQuota";

    /// <summary>Số loài khác nhau một khách xem được mỗi ngày. 0 = tắt hạn mức.</summary>
    public int DailySpeciesLimit { get; init; } = 20;

    /// <summary>
    /// Trần theo IP — chỉ để chặn scraper và người xoá localStorage lặp lại, KHÔNG phải hạn mức
    /// người dùng. Phải đặt cao: người dùng mobile VN nằm sau CGNAT của nhà mạng, hàng nghìn người
    /// dùng chung một IP. 0 = tắt.
    /// </summary>
    public int DailyIpSpeciesLimit { get; init; } = 300;

    /// <summary>Múi giờ (giờ lệch UTC) để chốt mốc reset nửa đêm. VN = +7, không có DST.</summary>
    public int ResetOffsetHours { get; init; } = 7;
}
