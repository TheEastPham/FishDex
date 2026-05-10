namespace UserManagement.Domain.Services.Interfaces;

public interface IEmailService
{
    Task<bool> SendEmailVerificationAsync(string email, string firstName, string verificationToken);
    Task<bool> SendPasswordResetAsync(string email, string firstName, string resetToken);
    Task<bool> SendWelcomeEmailAsync(string email, string firstName, string? language = null);
    Task<bool> SendVerificationCodeAsync(string email, string code, string? language = null);
}
