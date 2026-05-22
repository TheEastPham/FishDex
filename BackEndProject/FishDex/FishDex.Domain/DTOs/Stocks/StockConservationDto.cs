namespace FishDex.Domain.DTOs.Stocks;

public class StockConservationDto
{
    public int StockCode { get; init; }
    public string? IUCN_Code { get; init; }
    public string? IUCN_Assessment { get; init; }
    public DateTime? IUCN_DateAssessed { get; init; }
    public string? CITES_Code { get; init; }
}
