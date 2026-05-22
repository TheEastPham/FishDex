namespace FishDex.Domain.DTOs.Ecologies;

public class FeedingAndDietDto
{
    public int FeedingId { get; init; }
    public int EcologyId { get; init; }
    public string? FeedingType { get; init; }
    public decimal DietTroph { get; init; }
    public decimal FoodTroph { get; init; }
}
