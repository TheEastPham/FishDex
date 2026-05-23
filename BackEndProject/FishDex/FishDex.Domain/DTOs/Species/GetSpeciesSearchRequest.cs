namespace FishDex.Domain.DTOs.Species;

public class GetSpeciesSearchRequest
{
    public string Q { get; init; } = string.Empty;
    public Guid? FamId { get; init; }
    public int? GenusCode { get; init; }
    public string? Language { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
