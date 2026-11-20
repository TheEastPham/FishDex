namespace AquaHome.Domain.Exceptions;

/// <summary>
/// Ném khi content.json admin gửi lên sai cấu trúc (loại block lạ, thiếu field, quá dài,
/// assetId không thuộc bài...) — API filter map thành HTTP 422 kèm danh sách lỗi cho FE hiển thị.
/// </summary>
public class ArticleValidationException(IReadOnlyList<string> errors)
    : Exception(string.Join(" | ", errors))
{
    public IReadOnlyList<string> Errors { get; } = errors;
}
