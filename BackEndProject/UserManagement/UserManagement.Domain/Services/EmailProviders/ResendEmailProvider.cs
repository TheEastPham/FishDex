using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using UserManagement.Domain.Services.Interfaces;

namespace UserManagement.Domain.Services;

/// <summary>
/// Resend email provider - Simple and modern email API
/// https://resend.com/docs/send-with-dotnet
/// </summary>
public class ResendEmailProvider(
    IConfiguration configuration,
    ILogger<ResendEmailProvider> logger,
    IHttpClientFactory httpClientFactory) : IEmailProvider
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("ResendClient");
    private const string ResendApiUrl = "https://api.resend.com/emails";

    /// <summary>
    /// Send email via Resend API
    /// </summary>
    public async Task<bool> SendEmailAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string? fromEmail = null,
        string? fromName = null)
    {
        try
        {
            var apiKey = configuration["EmailSettings:Resend:ApiKey"];

            if (string.IsNullOrEmpty(apiKey))
            {
                logger.LogError("Resend API key not configured in EmailSettings:Resend:ApiKey");
                return false;
            }

            // Get from email/name from config or use defaults
            fromEmail ??= configuration["EmailSettings:FromEmail"] ?? "onboarding@resend.dev";
            fromName ??= configuration["EmailSettings:FromName"] ?? "FishLover System";

            // Build request payload
            var payload = new
            {
                from = $"{fromName} <{fromEmail}>",
                to = new[] { toEmail },
                subject = subject,
                html = htmlBody
            };

            var jsonContent = JsonSerializer.Serialize(payload);
            logger.LogDebug("Sending email via Resend to {Email}", toEmail);

            // Create request
            var request = new HttpRequestMessage(HttpMethod.Post, ResendApiUrl)
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            // Send request
            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                logger.LogInformation("Email sent successfully via Resend to {Email}. Response: {Response}",
                    toEmail, responseContent);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                logger.LogError("Resend API error {StatusCode}: {Error}",
                    response.StatusCode, errorContent);
                return false;
            }
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Network error sending email via Resend to {Email}", toEmail);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error sending email via Resend to {Email}", toEmail);
            return false;
        }
    }

    public string ProviderName => "Resend";
}
