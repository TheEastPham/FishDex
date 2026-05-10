namespace UserManagement.Domain.DTOs.Auth;

public class ValidateInvitationResponse
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid InvitationId { get; set; }
}