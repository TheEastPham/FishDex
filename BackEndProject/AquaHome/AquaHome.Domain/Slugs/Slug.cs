using System.Globalization;
using System.Text;

namespace AquaHome.Domain.Slugs;

/// <summary>
/// Slug cho URL công khai (bài viết, bể đã đăng) và tag. Một luật duy nhất cho mọi nơi:
/// bỏ dấu tiếng Việt (kể cả <c>đ</c>, thứ FormD không tách được), chữ thường, khoảng trắng và
/// <c>- _ . /</c> thành <c>-</c>, ký tự khác bỏ, gộp <c>--</c>, tối đa <see cref="MaxLength"/> ký tự.
/// Chữ không phải Latin (vd. tiếng Trung) được giữ nguyên.
/// </summary>
public static class Slug
{
    public const int MaxLength = 150;

    /// <summary>"Bể Đỏ của tôi" → "be-do-cua-toi". Trả chuỗi rỗng khi không còn ký tự nào dùng được.</summary>
    public static string From(string? text)
    {
        var normalized = (text ?? string.Empty).Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            // FormD không tách được đ/Đ — map tay, không thì chữ "đ" bị mất hoặc slug lọt ký tự phải percent-encode.
            if (c is 'đ') sb.Append('d');
            else if (char.IsLetterOrDigit(c)) sb.Append(c);
            else if (c is ' ' or '-' or '_' or '.' or '/') sb.Append('-');
        }

        var slug = sb.ToString().Normalize(NormalizationForm.FormC);
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        slug = slug.Trim('-');

        return slug.Length > MaxLength ? slug[..MaxLength].Trim('-') : slug;
    }

    /// <summary>
    /// Trả <paramref name="slug"/> nếu chưa ai dùng, không thì <c>slug-2</c>, <c>slug-3</c>… cho tới khi
    /// <paramref name="exists"/> báo trống. Kiểm rồi mới ghi nên hai request cùng lúc vẫn có thể ra cùng slug;
    /// chỗ ghi phải dựa vào unique index của DB.
    /// </summary>
    public static async Task<string> MakeUniqueAsync(string slug, Func<string, Task<bool>> exists)
    {
        if (!await exists(slug)) return slug;

        var suffix = 2;
        string candidate;
        do
        {
            candidate = $"{slug}-{suffix++}";
        } while (await exists(candidate));

        return candidate;
    }
}
