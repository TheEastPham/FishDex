using FishDex.EFCore.Entity.Market;

namespace FishDex.EFCore.Repository.Interface;

/// <summary>
/// Tra cứu trên index toàn bộ FishBase (~35.700 loài). Dùng để phân luồng UC1/UC2/UC3:
/// tìm thấy và <c>IsLoaded</c> → loài đã có trong FishDex; tìm thấy nhưng chưa nạp → cần
/// chạy ETL; không tìm thấy → loài lai, đi luồng community species.
///
/// <para><b>Đừng dùng bảng này để validate SpecCode khi ghi vào TradedSpecies</b> — nó không
/// chứa loài lai cộng đồng nên sẽ từ chối nhầm. Validate theo hai nhánh ở tầng service.</para>
/// </summary>
public interface IFishBaseSpeciesIndexRepository
{
    /// <summary>
    /// Tìm theo tên khoa học, không phân biệt hoa thường, khớp một phần.
    /// Sắp loài đã nạp lên trước vì đó là thứ người dùng chọn được ngay.
    /// </summary>
    Task<IReadOnlyList<FishBaseSpeciesIndex>> SearchAsync(
        string query, int limit = 20, CancellationToken ct = default);

    Task<FishBaseSpeciesIndex?> GetAsync(int specCode, CancellationToken ct = default);

    /// <summary>Các loài đã tra thấy trong FishBase nhưng chưa nạp vào FishDex — hàng đợi UC2.</summary>
    Task<IReadOnlyList<FishBaseSpeciesIndex>> GetNotLoadedAsync(
        IReadOnlyCollection<int> specCodes, CancellationToken ct = default);
}
