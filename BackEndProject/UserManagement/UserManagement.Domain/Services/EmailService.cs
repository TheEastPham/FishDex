using UserManagement.Domain.Services.Interfaces;
using UserManagement.Domain.Helper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace UserManagement.Domain.Services;

public class EmailService(
    IConfiguration configuration,
    ILogger<EmailService> logger,
    EmailTemplateHelper templateHelper,
    ResendEmailProvider emailProvider)
    : IEmailService
{
    private readonly IEmailProvider _emailProvider = emailProvider;

    public async Task<bool> SendEmailVerificationAsync(string email, string firstName, string verificationToken)
    {
        try
        {
            var baseUrl = configuration["AppSettings:BaseUrl"] ?? "http://localhost:5000";
            var verificationUrl = $"{baseUrl}/api/auth/verify-email?token={verificationToken}&email={Uri.EscapeDataString(email)}";

            var language = configuration["AppSettings:DefaultLanguage"] ?? "vi";

            var replacements = new Dictionary<string, string>
            {
                { "FirstName", firstName },
                { "VerificationUrl", verificationUrl },
                { "VerificationCode", verificationToken },
                { "Year", DateTime.Now.Year.ToString() }
            };

            var subject = await templateHelper.GetSubjectAsync("EmailVerification", language);
            var body = await templateHelper.GetTemplateAsync("EmailVerification", language, replacements);

            return await _emailProvider.SendEmailAsync(email, subject, body);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending email verification to {Email}", email);
            return false;
        }
    }

    public async Task<bool> SendPasswordResetAsync(string email, string firstName, string resetToken)
    {
        try
        {
            var baseUrl = configuration["AppSettings:BaseUrl"] ?? "http://localhost:5000";
            var resetUrl = $"{baseUrl}/api/auth/reset-password?token={resetToken}&email={Uri.EscapeDataString(email)}";

            var language = configuration["AppSettings:DefaultLanguage"] ?? "vi";

            var replacements = new Dictionary<string, string>
            {
                { "FirstName", firstName },
                { "ResetUrl", resetUrl },
                { "ResetToken", resetToken },
                { "Year", DateTime.Now.Year.ToString() }
            };

            var subject = await templateHelper.GetSubjectAsync("PasswordReset", language);
            var body = await templateHelper.GetTemplateAsync("PasswordReset", language, replacements);

            return await _emailProvider.SendEmailAsync(email, subject, body);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending password reset email to {Email}", email);
            return false;
        }
    }

    public async Task<bool> SendWelcomeEmailAsync(string email, string firstName, string? language = null)
    {
        try
        {
            language ??= configuration["AppSettings:DefaultLanguage"] ?? "vi";

            var replacements = new Dictionary<string, string>
            {
                { "FirstName", firstName },
                { "Year", DateTime.Now.Year.ToString() }
            };

            var subject = await templateHelper.GetSubjectAsync("WelcomeEmail", language);
            var body = await templateHelper.GetTemplateAsync("WelcomeEmail", language, replacements);

            var result = await _emailProvider.SendEmailAsync(email, subject, body);

            if (result)
            {
                logger.LogInformation("Welcome email sent successfully to {Email} in {Language}",
                    email, language);
            }

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending welcome email to {Email}", email);
            return false;
        }
    }

    public async Task<bool> SendVerificationCodeAsync(string email, string code, string? language = null)
    {
        try
        {
            language ??= configuration["AppSettings:DefaultLanguage"] ?? "vi";

            var replacements = new Dictionary<string, string>
            {
                { "Code", code },
                { "ExpiryTime", "5 phút" }, // or "5 minutes" for English
                { "Year", DateTime.Now.Year.ToString() }
            };

            var subject = await templateHelper.GetSubjectAsync("VerificationCode", language);
            var body = await templateHelper.GetTemplateAsync("VerificationCode", language, replacements);

            var result = await _emailProvider.SendEmailAsync(email, subject, body);

            if (result)
            {
                logger.LogInformation("Verification code sent successfully to {Email} in {Language}",
                    email, language);
            }

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending verification code to {Email}", email);
            return false;
        }
    }
}