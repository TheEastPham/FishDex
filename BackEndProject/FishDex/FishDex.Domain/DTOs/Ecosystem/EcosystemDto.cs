namespace FishDex.Domain.DTOs.Ecosystem;

public class EcosystemDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Type { get; init; }
    public string? Country { get; init; }
}
