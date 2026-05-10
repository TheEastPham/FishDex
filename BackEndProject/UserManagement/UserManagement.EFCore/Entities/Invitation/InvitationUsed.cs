namespace UserManagement.EFCore.Entities.Invitation;

public class InvitationUsed
{
    public Guid InvitationId { get; set; }
    public Guid UserId { get; set; }
    public DateTime UsedDate { get; set; }
}