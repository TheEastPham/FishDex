namespace AquaHome.Domain.Enums;

/// <summary>
/// Loại bài viết — chia theo giai đoạn người đọc đang đứng, không theo giọng văn:
/// chưa có bể → đang nuôi → muốn nuôi con gì / nhân giống → dùng app.
///
/// Cố ý giữ ít nhánh: chi tiết hơn (lọc, đèn, cycling, rêu hại, medaka...) đã có Tags lo,
/// mà tag thì thêm lúc soạn bài, còn enum mỗi lần thêm là phải sửa cả BE, FE và 4 file i18n.
/// Chỉ được THÊM vào cuối, không đổi thứ tự — số cũ đã nằm trong cột Articles.Type.
/// </summary>
public enum ArticleType
{
    /// <summary>Dựng bể, lọc/đèn/sưởi, cycling, nền, cây và bố cục.</summary>
    Setup = 0,

    /// <summary>Nước và thông số, cho ăn, bảo dưỡng định kỳ, bệnh, rêu hại.</summary>
    Care = 1,

    /// <summary>Chuyên sâu loài, ghép loài chung bể, ép đẻ, ương cá con.</summary>
    Species = 2,

    /// <summary>
    /// Thiên nhiên và cộng đồng: sinh cảnh ngoài tự nhiên của loài, chuyến đi thực địa,
    /// bảo tồn, câu chuyện của người chơi. Mục này để nối người nuôi với thiên nhiên và với
    /// nhau — khác hẳn ba mục kia vốn là kiến thức kỹ thuật.
    ///
    /// Hướng dẫn dùng app KHÔNG nằm ở đây: phần đó đi bằng video YouTube và chatbot, xem
    /// được thao tác thì hơn hẳn đọc chữ.
    /// </summary>
    Nature = 3,
}
