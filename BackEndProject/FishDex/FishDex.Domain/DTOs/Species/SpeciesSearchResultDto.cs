namespace FishDex.Domain.DTOs.Species;

public class SpeciesSearchResultDto
{
    public int SpecCode { get; init; }
    public string SpeciesName { get; init; } = string.Empty;
    public string? PreferredCommonName { get; init; }
    public string? GenusName { get; init; }
    public string? FamilyName { get; init; }
    /// <summary>Presigned URL cũ — giữ cho FE chưa lên bản media-access.</summary>
    public string? ImageUrl { get; init; }
    /// <summary>Object key system-image — FE ghép với SAS media-access.</summary>
    public string? ImageKey { get; init; }
}
