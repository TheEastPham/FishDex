namespace FishDex.Domain.DTOs.Ecosystem;

public class EcosystemDto
{
    public int AutoCtr { get; init; }
    public int E_CODE { get; init; }
    public int SpecCode { get; init; }
    public int? StockCode { get; init; }
    public string? Status { get; init; }
    public string? CurrentPresence { get; init; }
    public string? Abundance { get; init; }
    public string? LifeStage { get; init; }
    public string? Remarks { get; init; }
}
