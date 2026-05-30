namespace AquaHome.EFCore.Entity;

public class Aquarium
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public double? VolumeLiters { get; set; }
    public string? Type { get; set; }   // freshwater | saltwater | brackish
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<AquariumFish> Fish { get; set; } = [];
}
