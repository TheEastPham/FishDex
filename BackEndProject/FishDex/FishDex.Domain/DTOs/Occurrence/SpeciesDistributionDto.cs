namespace FishDex.Domain.DTOs.Occurrence;

public record SpeciesDistributionDto(
    int TotalOccurrences,
    IReadOnlyList<CountryDistributionDto> Countries
);
