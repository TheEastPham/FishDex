namespace FishDex.Domain.DTOs.Species;

public class GetSpeciesRequest
{
    public string? SearchTerm { get; init; }
    public int? FamCode { get; init; }
    public int? GenusCode { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
