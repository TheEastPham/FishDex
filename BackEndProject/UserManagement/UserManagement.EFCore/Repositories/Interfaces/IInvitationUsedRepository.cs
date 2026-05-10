using UserManagement.EFCore.Entities.Invitation;

namespace UserManagement.EFCore.Repositories.Interfaces;

public interface IInvitationUsedRepository
{
    Task<bool> CreateAsync(InvitationUsed invitationUsed);
}