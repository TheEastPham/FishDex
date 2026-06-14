namespace FishDex.Domain.DTOs.Species;

public class SpeciesSummaryDto
{
    public int SpecCode { get; init; }
    public string SpeciesName { get; init; } = string.Empty;
    public string? CommonName { get; init; }
    public string? ImageUrl { get; init; }
}
