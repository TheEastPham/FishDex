namespace AquaHome.EFCore.Entity;

public class RecentlyViewed
{
    public Guid UserId    { get; set; }
    public int  SpecCode  { get; set; }
    public DateTime ViewedAt { get; set; } = DateTime.UtcNow;
}
