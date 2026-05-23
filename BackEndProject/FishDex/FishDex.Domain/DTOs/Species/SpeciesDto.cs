namespace FishDex.Domain.DTOs.Species;

public class SpeciesDto
{
    public Guid Id { get; init; }
    public int SpecCode { get; init; }
    public string SpeciesName { get; init; } = string.Empty;
    public int? GenusCode { get; init; }
    public string? GenusName { get; init; }
    public string? FamilyName { get; init; }
}
