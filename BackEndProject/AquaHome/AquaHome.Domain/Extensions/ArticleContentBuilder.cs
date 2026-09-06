using AquaHome.Domain.DTOs;
using AquaHome.Domain.Exceptions;

namespace AquaHome.Domain.Extensions;

/// <summary>Kết quả normalize: nội dung sạch để ghi R2 + số liệu để lưu DB.</summary>
public record NormalizedArticleContent(
    ArticleContentDto Content,
    int BlockCount,
    int WordCount,
    int ReadingMinutes);

/// <summary>
/// Kiểm tra và chuẩn hóa nội dung admin gửi lên trước khi ghi content.json lên R2.
/// Mọi field không thuộc loại block đều bị set null — nhờ vậy FE chỉ phải xử lý đúng
/// những field mà loại block đó hứa có, và file trên R2 không lẫn dữ liệu rác.
/// </summary>
public static class ArticleContentBuilder
{
    public const int SchemaVersion = 1;

    private const int MaxIntroBlocks      = 10;
    private const int MaxBodyBlocks       = 200;
    private const int MaxConclusionBlocks = 10;
    private const int MaxParagraphChars   = 5000;
    private const int MaxHeadingChars     = 200;
    private const int MaxQuoteChars       = 1000;
    private const int MaxCiteChars        = 120;
    private const int MaxCaptionChars     = 300;
    private const int MaxAltChars         = 200;
    private const int MaxListItems        = 50;
    private const int MaxListItemChars    = 500;
    private const int WordsPerMinute      = 200;

    public static NormalizedArticleContent Normalize(
        ArticleContentInput? input, string template, IReadOnlyCollection<Guid> articleAssetIds)
    {
        var errors = new List<string>();
        var assetIds = articleAssetIds as ISet<Guid> ?? new HashSet<Guid>(articleAssetIds);

        var intro      = NormalizeSection(input?.Intro,      "intro",      MaxIntroBlocks,      assetIds, errors);
        var body       = NormalizeSection(input?.Body,       "body",       MaxBodyBlocks,       assetIds, errors);
        var conclusion = NormalizeSection(input?.Conclusion, "conclusion", MaxConclusionBlocks, assetIds, errors);

        if (errors.Count > 0)
            throw new ArticleValidationException(errors);

        var words = CountWords(intro) + CountWords(body) + CountWords(conclusion);

        return new NormalizedArticleContent(
            new ArticleContentDto(SchemaVersion, template, intro, body, conclusion),
            BlockCount: intro.Count + body.Count + conclusion.Count,
            WordCount: words,
            ReadingMinutes: Math.Max(1, (int)Math.Round(words / (double)WordsPerMinute, MidpointRounding.AwayFromZero)));
    }

    private static List<ArticleBlockDto> NormalizeSection(
        IReadOnlyList<ArticleBlockDto>? blocks, string section, int maxBlocks,
        ISet<Guid> assetIds, List<string> errors)
    {
        var result = new List<ArticleBlockDto>();
        if (blocks is null || blocks.Count == 0) return result;

        if (blocks.Count > maxBlocks)
        {
            errors.Add($"{section}: tối đa {maxBlocks} block, đang có {blocks.Count}");
            return result;
        }

        for (var i = 0; i < blocks.Count; i++)
        {
            var normalized = NormalizeBlock(blocks[i], $"{section}[{i}]", assetIds, errors);
            if (normalized is not null) result.Add(normalized);
        }

        return result;
    }

    private static ArticleBlockDto? NormalizeBlock(
        ArticleBlockDto block, string path, ISet<Guid> assetIds, List<string> errors)
    {
        var type = block.Type?.Trim().ToLowerInvariant();

        switch (type)
        {
            case ArticleBlockTypes.Paragraph:
            case ArticleBlockTypes.Tip:
            {
                var text = RequireText(block.Text, path, MaxParagraphChars, errors);
                return text is null ? null : new ArticleBlockDto(type, Text: text);
            }

            case ArticleBlockTypes.Heading:
            {
                var text = RequireText(block.Text, path, MaxHeadingChars, errors);
                // Chỉ h2/h3: h1 dành cho tiêu đề bài, xuống sâu hơn h3 thì mobile đọc không phân biệt được.
                var level = block.Level is 2 or 3 ? block.Level!.Value : 2;
                return text is null ? null : new ArticleBlockDto(type, Text: text, Level: level);
            }

            case ArticleBlockTypes.Quote:
            {
                var text = RequireText(block.Text, path, MaxQuoteChars, errors);
                var cite = Trim(block.Cite, MaxCiteChars);
                return text is null ? null : new ArticleBlockDto(type, Text: text, Cite: cite);
            }

            case ArticleBlockTypes.Image:
            {
                if (block.AssetId is null || block.AssetId == Guid.Empty)
                {
                    errors.Add($"{path}: block image thiếu assetId");
                    return null;
                }
                if (!assetIds.Contains(block.AssetId.Value))
                {
                    errors.Add($"{path}: assetId {block.AssetId} không thuộc bài viết này");
                    return null;
                }
                return new ArticleBlockDto(
                    type,
                    AssetId: block.AssetId,
                    Caption: Trim(block.Caption, MaxCaptionChars),
                    Alt: Trim(block.Alt, MaxAltChars));
            }

            case ArticleBlockTypes.List:
            {
                var items = (block.Items ?? [])
                    .Select(x => Trim(x, MaxListItemChars))
                    .Where(x => !string.IsNullOrEmpty(x))
                    .Cast<string>()
                    .ToList();

                if (items.Count == 0)
                {
                    errors.Add($"{path}: block list không có item nào");
                    return null;
                }
                if (items.Count > MaxListItems)
                {
                    errors.Add($"{path}: block list tối đa {MaxListItems} item");
                    return null;
                }
                return new ArticleBlockDto(type, Ordered: block.Ordered ?? false, Items: items);
            }

            default:
                errors.Add($"{path}: loại block '{block.Type}' không hợp lệ (chỉ nhận {string.Join(", ", ArticleBlockTypes.All)})");
                return null;
        }
    }

    private static string? RequireText(string? text, string path, int maxChars, List<string> errors)
    {
        var trimmed = text?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            errors.Add($"{path}: thiếu text");
            return null;
        }
        if (trimmed.Length > maxChars)
        {
            errors.Add($"{path}: text dài quá {maxChars} ký tự (đang {trimmed.Length})");
            return null;
        }
        return trimmed;
    }

    private static string? Trim(string? value, int maxChars)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed)) return null;
        return trimmed.Length > maxChars ? trimmed[..maxChars] : trimmed;
    }

    private static int CountWords(IEnumerable<ArticleBlockDto> blocks)
        => blocks.Sum(b =>
            CountWords(b.Text) + CountWords(b.Caption) + (b.Items?.Sum(CountWords) ?? 0));

    private static int CountWords(string? text)
        => string.IsNullOrWhiteSpace(text)
            ? 0
            : text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
}
