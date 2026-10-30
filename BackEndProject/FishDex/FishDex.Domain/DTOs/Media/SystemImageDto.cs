namespace FishDex.Domain.DTOs.Media;

public record SystemImageDto
{
    public Guid Id { get; init; }
    public int SpecCode { get; init; }
    public string Name { get; init; } = string.Empty;
    public string PictureType { get; init; } = string.Empty;
    public bool? PicPreferred { get; init; }
    public ImageGender Gender { get; init; }
    /// <summary>Presigned URL từ object storage. Null nếu storage chưa được cấu hình.</summary>
    public string? Url { get; init; }
    /// <summary>Object key system-image — FE ghép với SAS media-access. Url chỉ còn để tương thích bản cũ.</summary>
    public string? ObjectKey { get; init; }
}
