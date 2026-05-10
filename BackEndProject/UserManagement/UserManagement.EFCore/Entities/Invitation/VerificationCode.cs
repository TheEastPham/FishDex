namespace UserManagement.EFCore.Entities.Invitation;

public class VerificationCode
{
    public Guid? InvitationId { get; set; }
    public string Code { get; set; } = null!;
    public DateTime CreatedAt  { get; set; }
}