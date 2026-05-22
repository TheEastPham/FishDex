namespace FishDex.Domain.DTOs.Stocks;

public class StockEnvironmentDto
{
    public int StockCode { get; init; }
    public double? TempMin { get; init; }
    public double? TempMax { get; init; }
    public double? PHMin { get; init; }
    public double? PHMax { get; init; }
}
