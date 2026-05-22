namespace FishDex.Domain.DTOs.Species;

public class CommonNameDto
{
    public int AutoCtr { get; init; }
    public int SpecCode { get; init; }
    public string ComName { get; init; } = string.Empty;
    public string? Transliteration { get; init; }
    public string? Language { get; init; }
    public string? CountryCode { get; init; }
    public string? NameType { get; init; }
    public bool IsPreferred { get; init; }
    public int Rank { get; init; }
}
