namespace FishDex.Domain.DTOs.Species;

public class GenusDto
{
    public int GenusCode { get; init; }
    public string GenusName { get; init; } = string.Empty;
    public Guid FamId { get; init; }
}
