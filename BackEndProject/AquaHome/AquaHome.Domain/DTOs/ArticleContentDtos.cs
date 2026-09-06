namespace AquaHome.Domain.DTOs;

/// <summary>Các loại block hợp lệ trong content.json. BE từ chối mọi loại ngoài danh sách này.</summary>
public static class ArticleBlockTypes
{
    public const string Paragraph = "paragraph";
    public const string Heading   = "heading";
    public const string Image     = "image";
    public const string List      = "list";
    public const string Quote     = "quote";
    public const string Tip       = "tip";

    public static readonly string[] All = [Paragraph, Heading, Image, List, Quote, Tip];
}

/// <summary>
/// Một block nội dung. Union phẳng (field không dùng thì null) thay vì polymorphic JSON —
/// System.Text.Json đọc/ghi thẳng không cần converter, và FE switch theo Type là đủ.
/// BE normalize trước khi ghi R2: field không thuộc loại block đó luôn bị set null.
/// </summary>
public record ArticleBlockDto(
    string Type,
    string? Text = null,                       // paragraph | heading | quote | tip
    int? Level = null,                         // heading: 2 | 3
    Guid? AssetId = null,                      // image — trỏ tới ArticleAsset của chính bài này
    string? Caption = null,                    // image
    string? Alt = null,                        // image
    bool? Ordered = null,                      // list
    IReadOnlyList<string>? Items = null,       // list
    string? Cite = null);                      // quote

/// <summary>
/// Nội dung đầy đủ của một bản dịch — chính là file content.json trên R2. Template quyết định
/// thứ tự và cách render 3 mục; phase này chỉ có "standard": mở bài → thân bài → kết bài.
/// </summary>
public record ArticleContentDto(
    int SchemaVersion,
    string Template,
    IReadOnlyList<ArticleBlockDto> Intro,
    IReadOnlyList<ArticleBlockDto> Body,
    IReadOnlyList<ArticleBlockDto> Conclusion);

/// <summary>Phần nội dung admin gửi lên. SchemaVersion/Template do BE tự gán, FE không được quyết.</summary>
public record ArticleContentInput(
    IReadOnlyList<ArticleBlockDto>? Intro,
    IReadOnlyList<ArticleBlockDto>? Body,
    IReadOnlyList<ArticleBlockDto>? Conclusion);
