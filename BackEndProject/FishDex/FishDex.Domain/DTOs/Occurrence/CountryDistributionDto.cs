namespace FishDex.Domain.DTOs.Occurrence;

public record CountryDistributionDto(
    string Code,
    string Name,
    string? Alpha2,
    int Count,
    IReadOnlyList<OccurrencePointDto> Occurrences
);
