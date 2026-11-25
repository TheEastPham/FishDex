using System.Diagnostics.CodeAnalysis;

namespace FishLover.Storage.MediaAccess;

/// <summary>
/// Đưa object key về đúng một cách viết duy nhất trước khi ký presigned URL.
/// </summary>
/// <remarks>
/// SAS không ký path — client tự ghép key vào URL — nên key từ client phải không thể thoát ra ngoài
/// bucket. Decode đúng 1 lần rồi từ chối <c>%</c> còn sót, để traversal bị encode (<c>..%2F</c>) không lọt
/// được vào URL R2. Dùng chung cho mọi service có media-access.
/// </remarks>
public static class AssetKeyCanonicalizer
{
    public const int MaxLength = 1024;

    private const char Separator = '/';

    private static readonly char[] ForbiddenCharacters = ['\\', '%', '*', '?', '#'];

    /// <summary>Không có '/' đầu/cuối, không có segment rỗng, mỗi segment decode đúng 1 lần.</summary>
    public static bool TryCanonicalizePath(string? input, [NotNullWhen(true)] out string? canonical)
    {
        canonical = null;

        if (!TryCanonicalizeSegments(input, out var segments))
            return false;

        canonical = string.Join(Separator, segments);
        return true;
    }

    private static bool TryCanonicalizeSegments(string? input, out List<string> segments)
    {
        if (!TryCanonicalizeSegmentsOnce(input, out segments))
            return false;

        // Dạng chuẩn phải là điểm bất động: vd khoảng trắng bị encode ở mép segment sẽ bị Trim ở lượt sau
        // → key ký ra khác key giao đi.
        var joined = string.Join(Separator, segments);
        return TryCanonicalizeSegmentsOnce(joined, out var again)
               && string.Join(Separator, again) == joined;
    }

    private static bool TryCanonicalizeSegmentsOnce(string? input, out List<string> segments)
    {
        segments = [];

        var trimmed = input?.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.Length > MaxLength)
            return false;

        foreach (var rawSegment in trimmed.Split(Separator, StringSplitOptions.RemoveEmptyEntries))
        {
            var segment = DecodeOnce(rawSegment);

            // '/' bị encode sẽ decode ra bên trong segment và giấu "../" khỏi các check dưới.
            if (segment is "." or ".."
                || segment.Contains(Separator)
                || segment.IndexOfAny(ForbiddenCharacters) >= 0
                || segment.Any(char.IsControl))
                return false;

            segments.Add(segment);
        }

        if (segments.Count == 0)
            return false;

        // Có scheme ở segment đầu ("https:", "file:", "C:") = client gửi URL/đường dẫn ổ đĩa, không phải key.
        return !segments[0].Contains(':')
               && !Uri.TryCreate(string.Join(Separator, segments), UriKind.Absolute, out _);
    }

    private static string DecodeOnce(string segment)
    {
        try
        {
            return Uri.UnescapeDataString(segment);
        }
        catch (UriFormatException)
        {
            return segment;
        }
    }
}
