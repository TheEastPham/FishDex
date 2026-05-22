namespace FishDex.Domain.DTOs.Species;

public class FamilyDto
{
    public Guid Id { get; init; }
    public int FamCode { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? CommonName { get; init; }
}
