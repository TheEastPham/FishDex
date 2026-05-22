using System.ComponentModel.DataAnnotations;

namespace FishDex.EFCore.Entity.Species;

public class CommonName
{
    [Key]
    public int AutoCtr { get; set; }
    public int SpecCode { get; set; }
    public int? StockCode { get; set; }
    public string ComName { get; set; } = string.Empty;
    public string? Transliteration { get; set; }
    public string? CountryCode { get; set; }
    public string? Language { get; set; }
    public string? NameType { get; set; }
    public bool IsPreferred { get; set; }
    public int Rank { get; set; }
    public string? Remarks { get; set; }

    public virtual Species Species { get; set; } = null!;
}
