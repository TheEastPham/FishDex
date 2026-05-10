namespace UserManagement.Domain.Services.Interfaces;

public interface IEmailProvider
{
    /// <summary>
    /// Send email using the provider
    /// </summary>
    Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, string? fromEmail = null, string? fromName = null);
    
    /// <summary>
    /// Provider name for logging/debugging
    /// </summary>
    string ProviderName { get; }
}