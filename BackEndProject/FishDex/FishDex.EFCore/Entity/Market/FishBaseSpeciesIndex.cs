namespace FishDex.EFCore.Entity.Market;

/// <summary>
/// Index nhẹ của TOÀN BỘ loài trong FishBase (~35.700 dòng), không chỉ 2.043 loài đã lọc vào
/// bảng <c>Species</c>. Không ảnh, không quan hệ — chỉ đủ để tra tên và biết loài đó đã được
/// nạp vào FishDex hay chưa.
///
/// <para><b>Vì sao cần:</b> khi người dùng tìm một loài không có trong danh sách market,
/// hệ thống phải biết loài đó có tồn tại trong FishBase hay không. Nếu không có bảng này thì
/// không phân biệt được "loài thật chưa nạp" với "loài lai chưa từng có", và admin phải đi
/// đối chiếu tay như hiện nay.</para>
///
/// <para><b>Cột <see cref="IsLoaded"/> gộp ba trường hợp về một câu truy vấn:</b>
/// có và đã nạp → thêm thẳng vào danh sách quốc gia;
/// có nhưng chưa nạp → cần chạy ETL, và đã có sẵn SpecCode chính xác để gom lô;
/// không có → loài lai, đi luồng community species.</para>
///
/// <para><b>Vì sao nạp vào Postgres thay vì đọc parquet lúc chạy:</b> parquet không có index nên
/// search chậm, và sẽ phải mang cả thư mục 200 file lên server. Nạp một lần thì "tool" chỉ là
/// một loader nữa trong <c>etl/loaders/</c> đúng pattern sẵn có.</para>
///
/// <para><b>V1 không gồm tên đồng nghĩa.</b> <c>synonyms.parquet</c> có 102.732 dòng và người
/// nuôi cá hay tra bằng tên khoa học đã lỗi thời — nhưng để sau, đo tỷ lệ tìm không thấy thật
/// rồi hãy quyết.</para>
/// </summary>
public class FishBaseSpeciesIndex
{
    public int SpecCode { get; set; }

    /// <summary>Tên khoa học đầy đủ, ghép từ Genus + Species của parquet.</summary>
    public string SpeciesName { get; set; } = string.Empty;

    public string? Genus { get; set; }

    /// <summary>
    /// Mã họ. Không lưu tên họ vì bảng <c>Families</c> được ETL nạp TOÀN BỘ không lọc,
    /// nên join lấy tên lúc cần là đủ.
    /// </summary>
    public int? FamCode { get; set; }

    public bool Fresh { get; set; }
    public bool Brack { get; set; }

    /// <summary>
    /// Giá trị thô cột <c>Aquarium</c> của FishBase: highly commercial, commercial, potential,
    /// public aquariums, show aquarium, never/rarely, hoặc null. Đây chính là cột mà ETL dùng
    /// để lọc ra 2.043 loài nước ngọt vào DB.
    /// </summary>
    public string? Aquarium { get; set; }

    /// <summary>
    /// true nếu loài đã có row trong bảng <c>Species</c> của FishDex. Loader tự set lại
    /// mỗi lần chạy bằng cách đối chiếu với bảng đó, không tin giá trị cũ.
    /// </summary>
    public bool IsLoaded { get; set; }
}
