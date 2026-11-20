namespace AquaHome.EFCore.Entity;

/// <summary>
/// Giới hạn tài nguyên theo role (Story 3.5 — Quota Engine).
/// Giá trị -1 = không giới hạn.
/// </summary>
public class RoleQuota
{
    public string Role { get; set; } = string.Empty;
    public int MaxFavorites { get; set; }
    public int MaxAquariums { get; set; }
    public int SearchPerDay { get; set; }
    public int AiQaPerDay { get; set; }
    public int ImageSearchPerDay { get; set; }
    public DateTime UpdatedAt { get; set; }
}
