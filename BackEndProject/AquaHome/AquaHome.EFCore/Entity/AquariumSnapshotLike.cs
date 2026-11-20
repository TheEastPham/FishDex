namespace AquaHome.EFCore.Entity;

public class AquariumSnapshotLike
{
    public Guid Id { get; set; }
    public Guid SnapshotId { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; }

    public AquariumSnapshot Snapshot { get; set; } = null!;
}
