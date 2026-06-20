namespace FishDex.Domain.DTOs.Ecologies;

public class SubstrateDto
{
    public int EcologyId { get; init; }
    public IReadOnlyList<string> PreferredSubstrates { get; init; } = [];
    public bool BurrowingCapable { get; init; }
}
