namespace UserManagement.Domain.Models;

/// <summary>
/// User profile domain model
/// </summary>
public class UserProfile
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? Bio { get; set; }
    public Dictionary<string, object> Preferences { get; set; } = new();
    public DateTime UpdatedAt { get; set; }
}
