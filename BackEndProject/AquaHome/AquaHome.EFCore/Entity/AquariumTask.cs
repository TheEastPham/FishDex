using AquaHome.Common.Enums;

namespace AquaHome.EFCore.Entity;

public class AquariumTask
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid AquariumId { get; set; }
    public AquaTaskType AquaTaskType { get; set; }
    public int? IntervalDays { get; set; }
    public DateTime DueAt { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool Reminded { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Aquarium? Aquarium { get; set; }
}
