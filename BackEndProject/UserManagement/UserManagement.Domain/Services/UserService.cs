using UserManagement.Domain.DTOs.User;
using UserManagement.Domain.DTOs.Role;
using UserManagement.Domain.Mappings;
using UserManagement.Domain.Services.Interfaces;
using UserManagement.EFCore.Entities.User;
using UserManagement.EFCore.Repositories.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;

namespace UserManagement.Domain.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly UserManager<UserEntity> _userManager;
    private readonly IOpenIddictAuthorizationManager _authorizationManager;
    private readonly IOpenIddictTokenManager _tokenManager;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepository,
        UserManager<UserEntity> userManager,
        IOpenIddictAuthorizationManager authorizationManager,
        IOpenIddictTokenManager tokenManager,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _userManager = userManager;
        _authorizationManager = authorizationManager;
        _tokenManager = tokenManager;
        _logger = logger;
    }

    public async Task<GetUsersResponse> GetUsersAsync(GetUsersRequest request)
    {
        try
        {
            var userEntities = await _userRepository.GetAllAsync(request.Page, request.PageSize, request.Role);
            var totalCount = await _userRepository.CountAsync(request.Role);

            var users = new List<UserDto>();
            foreach (var userEntity in userEntities)
            {
                var roles = await _userManager.GetRolesAsync(userEntity);
                users.Add(userEntity.ToUser(roles).ToDto());
            }

            var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

            return new GetUsersResponse(users, totalCount, request.Page, request.PageSize, totalPages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users for page {Page}", request.Page);
            throw;
        }
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid id)
    {
        try
        {
            var userEntity = await _userRepository.GetByIdAsync(id);
            if (userEntity == null) return null;

            var roles = await _userManager.GetRolesAsync(userEntity);
            return userEntity.ToUser(roles).ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserId}", id);
            throw;
        }
    }

    public async Task<UserDto> CreateUserAsync(CreateUserRequest request)
    {
        ValidateArgument(request);
        try
        {
            var userEntity = new UserEntity
            {
                UserName    = request.Email,
                Email       = request.Email,
                FirstName   = request.FirstName,
                LastName    = request.LastName,
                DateOfBirth = request.DateOfBirth,
                TimeZone    = request.TimeZone,
                Language    = request.Language,
                CreatedAt   = DateTime.UtcNow,
                UpdatedAt   = DateTime.UtcNow,
                IsActive    = false
            };

            var result = await _userManager.CreateAsync(userEntity, request.Password);
            if (result == null || !result.Succeeded)
            {
                var errors = result?.Errors?.Select(e => e.Description) ?? ["Unknown error occurred."];
                throw new InvalidOperationException($"Failed to create user: {string.Join(", ", errors)}");
            }

            return userEntity.ToUser([]).ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user {Email}", request.Email);
            throw;
        }
    }

    public async Task<UserDto> UpdateUserAsync(UpdateUserRequest request)
    {
        try
        {
            var userEntity = await _userRepository.GetByIdAsync(request.Id);
            if (userEntity == null)
                throw new InvalidOperationException($"User with ID {request.Id} not found");

            userEntity.FirstName   = request.FirstName;
            userEntity.LastName    = request.LastName;
            userEntity.DateOfBirth = request.DateOfBirth;
            userEntity.Avatar      = request.Avatar;
            userEntity.TimeZone    = request.TimeZone;
            userEntity.Language    = request.Language;
            userEntity.UpdatedAt   = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(request.PhoneNumber))
                userEntity.PhoneNumber = request.PhoneNumber;

            if (!string.IsNullOrEmpty(request.Email) && request.Email != userEntity.Email)
            {
                userEntity.Email            = request.Email;
                userEntity.NormalizedEmail  = request.Email.ToUpperInvariant();
                userEntity.UserName         = request.Email;
                userEntity.NormalizedUserName = request.Email.ToUpperInvariant();
            }

            await _userRepository.UpdateAsync(userEntity);

            var roles = await _userManager.GetRolesAsync(userEntity);
            return userEntity.ToUser(roles).ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", request.Id);
            throw;
        }
    }

    public async Task<bool> DeleteUserAsync(Guid id)
    {
        try
        {
            if (!await _userRepository.DeleteAsync(id)) return false;

            await RevokeSessionsAsync(id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", id);
            throw;
        }
    }

    /// <summary>
    /// Vô hiệu hoá chỉ set <c>IsActive = false</c> thì mọi phiên đang có vẫn sống: refresh token OpenIddict
    /// (và refresh token legacy) vẫn đổi được access token mới. Thu hồi hết ở đây. Access token đã cấp (JWT,
    /// FishDex/AquaHome tự verify) vẫn dùng được tới khi hết hạn (mặc định 1 giờ).
    /// </summary>
    private async Task RevokeSessionsAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is not null)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            // Đổi stamp + lưu luôn 2 field trên: cookie đăng nhập ở /connect bị từ chối ở lần validate kế tiếp.
            await _userManager.UpdateSecurityStampAsync(user);
        }

        var subject = userId.ToString();

        // Gom hết rồi mới revoke: mỗi lần revoke là 1 SaveChanges, không chạy được khi DbContext còn đang
        // stream kết quả FindBySubjectAsync.
        var authorizations = new List<object>();
        await foreach (var authorization in _authorizationManager.FindBySubjectAsync(subject))
            authorizations.Add(authorization);
        foreach (var authorization in authorizations)
            await _authorizationManager.TryRevokeAsync(authorization);

        var tokens = new List<object>();
        await foreach (var token in _tokenManager.FindBySubjectAsync(subject))
            tokens.Add(token);
        foreach (var token in tokens)
            await _tokenManager.TryRevokeAsync(token);

        _logger.LogInformation("Revoked {Authorizations} authorizations and {Tokens} tokens of deactivated user {UserId}",
            authorizations.Count, tokens.Count, userId);
    }

    public async Task<bool> AssignRoleAsync(AssignRoleRequest request)
    {
        try
        {
            if (!request.IsValidRole()) return false;

            var userEntity = await _userRepository.GetByIdAsync(request.UserId);
            if (userEntity == null) return false;

            var result = await _userManager.AddToRoleAsync(userEntity, request.RoleName);
            return result.Succeeded;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role {RoleName} to user {UserId}", request.RoleName, request.UserId);
            throw;
        }
    }

    public async Task<bool> RemoveRoleAsync(Guid userId, string roleName)
    {
        try
        {
            var userEntity = await _userRepository.GetByIdAsync(userId);
            if (userEntity == null) return false;

            var result = await _userManager.RemoveFromRoleAsync(userEntity, roleName);
            return result.Succeeded;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing role {RoleName} from user {UserId}", roleName, userId);
            throw;
        }
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
    {
        try
        {
            var userEntity = await _userManager.FindByIdAsync(userId.ToString());
            if (userEntity == null)
                throw new InvalidOperationException($"User with ID {userId} not found.");

            var result = await _userManager.ChangePasswordAsync(userEntity, currentPassword, newPassword);
            return result.Succeeded;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {UserId}", userId);
            throw;
        }
    }

    private static void ValidateArgument(CreateUserRequest request)
    {
        if (request == null)
            throw new ArgumentException(nameof(request));
        if (string.IsNullOrEmpty(request.Email))
            throw new ArgumentException(nameof(request.Email));
        if (string.IsNullOrEmpty(request.Password))
            throw new ArgumentException(nameof(request.Password));
        if (string.IsNullOrEmpty(request.FirstName))
            throw new ArgumentException(nameof(request.FirstName));
        if (request.LastName == null)
            throw new ArgumentException(nameof(request.LastName));
    }
}
