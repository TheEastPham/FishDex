using Microsoft.EntityFrameworkCore;
using UserManagement.EFCore.Data;
using UserManagement.EFCore.Entities.Invitation;
using UserManagement.EFCore.Repositories.Interfaces;

namespace UserManagement.EFCore.Repositories;

public class InvitationRepository(UserManagementDbContext context) : IInvitationRepository
{
    public Task<Invitation?> GetByCodeAsync(string code)
    {
        return context.Invitations.FirstOrDefaultAsync(i => i.Code == code);
    }
}