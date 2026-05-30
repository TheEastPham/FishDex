namespace FishDex.Domain.DTOs.Species;

public class SpeciesSearchResultDto
{
    public int SpecCode { get; init; }
    public string SpeciesName { get; init; } = string.Empty;
    public string? PreferredCommonName { get; init; }
    public string? GenusName { get; init; }
    public string? FamilyName { get; init; }
    public string? ImageUrl { get; init; }
}
