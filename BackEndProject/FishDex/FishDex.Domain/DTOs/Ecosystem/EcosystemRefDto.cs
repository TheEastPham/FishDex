namespace FishDex.Domain.DTOs.Ecosystem;

public class EcosystemRefDto
{
    public int E_CODE { get; init; }
    public string? EcosystemName { get; init; }
    public string? EcosystemType { get; init; }
    public string? Location { get; init; }
    public double? NorthernLat { get; init; }
    public double? SouthernLat { get; init; }
    public double? WesternLat { get; init; }
    public double? EasternLat { get; init; }
    public double? Area { get; init; }
    public double? DrainageArea { get; init; }
    public double? RiverLength { get; init; }
    public double? Salinity { get; init; }
    public double? AverageDepth { get; init; }
    public double? MaxDepth { get; init; }
    public double? TempSurface { get; init; }
    public double? TempDepth { get; init; }
    public bool Polar { get; init; }
    public bool Boreal { get; init; }
    public bool Temperate { get; init; }
    public bool Subtropical { get; init; }
    public bool Tropical { get; init; }
    public string? MEOW { get; init; }
    public string? LME { get; init; }
    public string? MPA { get; init; }
    public int? TotalCount { get; init; }
    public int? TotalFamCount { get; init; }
    public string? Description { get; init; }
    public string? Comments { get; init; }
    public DateTime? LastUpdate { get; init; }
}
