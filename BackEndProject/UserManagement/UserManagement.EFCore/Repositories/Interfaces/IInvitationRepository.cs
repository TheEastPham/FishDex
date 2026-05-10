using UserManagement.EFCore.Entities.Invitation;

namespace UserManagement.EFCore.Repositories.Interfaces;

public interface IInvitationRepository
{
    Task<Invitation?> GetByCodeAsync(string code);
}