namespace FishDex.Domain.DTOs.Species;

public class SpeciesDetailDto
{
    public int SpecCode { get; init; }
    public string SpeciesName { get; init; } = string.Empty;
    public string? PreferredCommonName { get; init; }
    public string? GenusName { get; init; }
    public string? FamilyName { get; init; }
    public string? WaterType { get; init; }
    public decimal? Length { get; init; }
    public decimal? Weight { get; init; }
    public string? Dangerous { get; init; }
    public string? DemersPelag { get; init; }
    public string? LifeCycle { get; init; }
    public string? Remark { get; init; }

    public string? PreferredImageUrl { get; init; }
    public string? MaleImageUrl { get; init; }
    public string? FemaleImageUrl { get; init; }

    public SpeciesDetailEcologyDto? Ecology { get; init; }
    public SpeciesDetailConservationDto? Conservation { get; init; }
    public SpeciesDetailEnvironmentDto? Environment { get; init; }
}

public class SpeciesDetailEcologyDto
{
    public string? FeedingType { get; init; }
    public decimal? DietTroph { get; init; }
    public IReadOnlyList<string> HabitatZones { get; init; } = [];
}

public class SpeciesDetailConservationDto
{
    public string? IucnCode { get; init; }
    public string? IucnAssessment { get; init; }
    public DateTime? IucnDateAssessed { get; init; }
    public string? CitesCode { get; init; }
}

public class SpeciesDetailEnvironmentDto
{
    public double? TempMin { get; init; }
    public double? TempMax { get; init; }
    public double? PhMin { get; init; }
    public double? PhMax { get; init; }
}
