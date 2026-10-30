namespace FishDex.Domain.DTOs.Species;

public class SpeciesSummaryDto
{
    public int SpecCode { get; init; }
    public string SpeciesName { get; init; } = string.Empty;
    public string? CommonName { get; init; }
    /// <summary>Presigned URL cũ — giữ cho FE chưa lên bản media-access. Xoá khi mọi client đã dùng ImageKey.</summary>
    public string? ImageUrl { get; init; }

    /// <summary>Object key trong system-image — bất biến, lưu/cache được. FE ghép với SAS media-access.</summary>
    public string? ImageKey { get; init; }
}
