using UserManagement.EFCore.Data;
using UserManagement.EFCore.Entities.User;
using UserManagement.EFCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace UserManagement.EFCore.Repositories;

public class UserRepository(UserManagementDbContext context) : IUserRepository
{
    public async Task<UserEntity?> GetByIdAsync(Guid id)
    {
        return await context.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<UserEntity?> GetByEmailAsync(string email)
    {
        return await context.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<IEnumerable<UserEntity>> GetAllAsync(int page = 1, int pageSize = 20, string? role = null)
    {
        var query = context.Users
            .Include(u => u.Profile)
            .AsQueryable();

        if (!string.IsNullOrEmpty(role))
        {
            var usersInRole = await context.UserRoles
                .Where(ur => context.Roles.Any(r => r.Id == ur.RoleId && r.Name == role))
                .Select(ur => ur.UserId)
                .ToListAsync();

            query = query.Where(u => usersInRole.Contains(u.Id));
        }

        return await query
            .OrderBy(u => u.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<UserEntity> CreateAsync(UserEntity user)
    {
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    public async Task<UserEntity> UpdateAsync(UserEntity user)
    {
        user.UpdatedAt = DateTime.UtcNow;
        context.Users.Update(user);
        await context.SaveChangesAsync();
        return user;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var user = await context.Users.FindAsync(id);
        if (user == null) return false;

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<int> CountAsync(string? role = null)
    {
        var query = context.Users.AsQueryable();

        if (!string.IsNullOrEmpty(role))
        {
            var usersInRole = await context.UserRoles
                .Where(ur => context.Roles.Any(r => r.Id == ur.RoleId && r.Name == role))
                .Select(ur => ur.UserId)
                .ToListAsync();

            query = query.Where(u => usersInRole.Contains(u.Id));
        }

        return await query.CountAsync();
    }
}
