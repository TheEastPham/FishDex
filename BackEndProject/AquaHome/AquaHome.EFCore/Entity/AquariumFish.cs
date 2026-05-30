namespace AquaHome.EFCore.Entity;

public class AquariumFish
{
    public Guid AquariumId { get; set; }
    public int SpecCode { get; set; }
    public int Quantity { get; set; } = 1;
    public DateTime AddedAt { get; set; }

    public Aquarium Aquarium { get; set; } = null!;
}
