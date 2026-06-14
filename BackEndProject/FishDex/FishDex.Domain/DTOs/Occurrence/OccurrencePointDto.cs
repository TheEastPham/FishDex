namespace FishDex.Domain.DTOs.Occurrence;

public record OccurrencePointDto(double Lat, double Lon, string? Locality, string? Province);
