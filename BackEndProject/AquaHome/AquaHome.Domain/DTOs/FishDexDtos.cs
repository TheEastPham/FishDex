namespace AquaHome.Domain.DTOs;

public record SpeciesSummaryDto(
    int SpecCode,
    string SpeciesName,
    string? CommonName,
    string? ImageUrl);

public record DistributionPointDto(
    double LatitudeDec,
    double LongitudeDec,
    string? CountryCode,
    string? Locality);
