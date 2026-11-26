namespace AquaHome.Domain.Articles;

/// <summary>Ngôn ngữ của bài viết — khớp locale của FE.</summary>
public static class ArticleLanguages
{
    /// <summary>Ngôn ngữ được nhận. Ngoài danh sách: khi đọc thì coi như <c>vi</c>, khi soạn bài thì bị từ chối.</summary>
    public static readonly IReadOnlyList<string> Supported = ["vi", "en", "de", "zh"];

    /// <summary>Thiếu bản dịch đang xem thì lùi theo thứ tự này (chốt cùng FE), cuối cùng là bản bất kỳ theo thứ tự chữ cái.</summary>
    internal static readonly IReadOnlyList<string> Fallback = ["en", "vi"];

    /// <summary>"vi-VN"/"en-US" → "vi"/"en" (FE gửi locale đầy đủ); rỗng hoặc không hỗ trợ → "vi".</summary>
    public static string Normalize(string? language)
    {
        var trimmed = language?.Trim();
        if (string.IsNullOrEmpty(trimmed)) return "vi";

        var code = trimmed[..Math.Min(2, trimmed.Length)].ToLowerInvariant();
        return Supported.Contains(code) ? code : "vi";
    }
}
