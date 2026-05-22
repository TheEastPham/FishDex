namespace FishDex.Domain.DTOs.Stocks;

public class StockDto
{
    public int StockCode { get; init; }
    public int SpecCode { get; init; }
    public string? StockDefs { get; init; }
    public string? Level { get; init; }
}
