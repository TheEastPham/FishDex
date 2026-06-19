namespace FishDex.Domain.DTOs.Ecologies;

public class SpecialHabitatDto
{
    public int EcologyId { get; init; }
    public IReadOnlyList<string> SpecialHabitats { get; init; } = [];
    public bool RequiresCaves { get; init; }
    public bool RequiresDriftwood { get; init; }
    public bool RequiresVegetation { get; init; }
    public bool RequiresCoralReefs { get; init; }
}
