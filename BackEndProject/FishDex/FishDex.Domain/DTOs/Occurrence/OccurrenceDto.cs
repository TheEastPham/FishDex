namespace FishDex.Domain.DTOs.Occurrence;

public class OccurrenceDto
{
    public long Id { get; init; }
    public int SpecCode { get; init; }
    public string? CountryCode { get; init; }
    public string? Locality { get; init; }
    public double LatitudeDec { get; init; }
    public double LongitudeDec { get; init; }
    public string? Province { get; init; }
}
