namespace AquaHome.EFCore.Entity;

public class UserFavorite
{
    public Guid UserId { get; set; }
    public int SpecCode { get; set; }
    public DateTime AddedAt { get; set; }
}
