using UserManagement.Domain.DTOs.User;
using UserManagement.Domain.DTOs.Role;

namespace UserManagement.Domain.Services.Interfaces;

public interface IUserService
{
    Task<GetUsersResponse> GetUsersAsync(GetUsersRequest request);
    Task<UserDto?> GetUserByIdAsync(Guid id);
    Task<UserDto?> GetUserByEmailAsync(string email);
    Task<UserDto> CreateUserAsync(CreateUserRequest request);
    Task<UserDto> UpdateUserAsync(UpdateUserRequest request);
    Task<bool> DeleteUserAsync(Guid id);
    Task<bool> AssignRoleAsync(AssignRoleRequest request);
    Task<bool> RemoveRoleAsync(Guid userId, string roleName);
    Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
}
