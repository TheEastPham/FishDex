namespace UserManagement.EFCore.Entities.Invitation;

public class Invitation
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public DateTime CreationDate { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime ExpiryDate  { get; set; }
    public int MaxUses { get; set; }
    public virtual ICollection<InvitationUsed> UsedBy { get; set; } = new List<InvitationUsed>();
}