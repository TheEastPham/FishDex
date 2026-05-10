namespace FishDex.EFCore.Entity.Occurrence;

public class Occurrence
{
    public long Id { get; set; }
    public int SpecCode { get; set; }
    public string? CountryCode { get; set; }
    public string? Locality { get; set; }
    public string? Gazetteer { get; set; }
    public double LatitudeDec  { get; set; }
    public double LongitudeDec   { get; set; }
    public string? Province { get; set; }
}