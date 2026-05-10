using UserManagement.EFCore.Data;
using UserManagement.EFCore.Entities.Invitation;
using UserManagement.EFCore.Repositories.Interfaces;

namespace UserManagement.EFCore.Repositories;

public class InvitationUsedRepository(UserManagementDbContext context) : IInvitationUsedRepository
{
    public async Task<bool> CreateAsync(InvitationUsed invitationUsed)
    {
        await context.InvitationUsages.AddAsync(invitationUsed);
        return await context.SaveChangesAsync() > 0;
    }
}