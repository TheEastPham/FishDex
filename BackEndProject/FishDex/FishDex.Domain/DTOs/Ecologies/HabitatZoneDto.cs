namespace FishDex.Domain.DTOs.Ecologies;

public class HabitatZoneDto
{
    public int HabitatZoneId { get; init; }
    public int EcologyId { get; init; }
    public bool Neritic { get; init; }
    public bool Estuaries { get; init; }
    public bool Mangroves { get; init; }
    public bool Stream { get; init; }
    public bool Lakes { get; init; }
}
