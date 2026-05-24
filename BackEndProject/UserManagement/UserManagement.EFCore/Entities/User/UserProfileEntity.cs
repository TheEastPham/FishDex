namespace UserManagement.EFCore.Entities.User;

/// <summary>
/// User profile entity for database
/// </summary>
public class UserProfileEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? Bio { get; set; }
    public string? Preferences { get; set; } // JSON string for EF Core
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual UserEntity User { get; set; } = null!;
}
