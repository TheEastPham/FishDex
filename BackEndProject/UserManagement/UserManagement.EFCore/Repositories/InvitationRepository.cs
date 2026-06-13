using Microsoft.EntityFrameworkCore;
using UserManagement.EFCore.Data;
using UserManagement.EFCore.Entities.Invitation;
using UserManagement.EFCore.Repositories.Interfaces;

namespace UserManagement.EFCore.Repositories;

public class InvitationRepository(UserManagementDbContext context) : IInvitationRepository
{
    public Task<Invitation?> GetByCodeAsync(string code)
        => context.Invitations.FirstOrDefaultAsync(i => i.Code == code);

    public Task<int> GetUsageCountAsync(Guid invitationId)
        => context.InvitationUsages.CountAsync(u => u.InvitationId == invitationId);
}