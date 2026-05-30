namespace FishDex.Domain.DTOs.Ecologies;

public class EcologyDto
{
    public int EcologyId { get; init; }
    public int SpecCode { get; init; }
    public string? StockCode { get; init; }
    public bool? Schooling { get; init; }
    public bool? Shoaling { get; init; }
    public bool? Solitary { get; init; }
}
