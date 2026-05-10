using UserManagement.Domain.DTOs.Auth;
using UserManagement.Domain.DTOs.Account;

namespace UserManagement.Domain.Services.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<TokenResponse> RefreshTokenAsync(RefreshTokenRequest request);
    Task<bool> LogoutAsync(Guid userId);
    Task<RegisterResponse> RegisterAsync(RegisterRequest request);
    Task<EmailVerificationResponse> GetVerificationCode(string email, string? invitationCode);
}
