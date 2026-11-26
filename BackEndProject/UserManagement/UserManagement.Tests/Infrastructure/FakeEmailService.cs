using System.Collections.Concurrent;
using UserManagement.Domain.Services.Interfaces;

namespace UserManagement.Tests.Infrastructure;

/// <summary>
/// Thay <see cref="IEmailService"/> thật (Resend): không gửi gì, chỉ giữ lại thứ đã "gửi"
/// để test đọc mã OTP và link đặt lại mật khẩu như người dùng đọc trong hộp thư.
/// </summary>
public sealed class FakeEmailService : IEmailService
{
    private readonly ConcurrentDictionary<string, string> _codes = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _resetUrls = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentBag<string> _welcomed = [];

    public string? LastCodeFor(string email) => _codes.GetValueOrDefault(email);
    public string? LastResetUrlFor(string email) => _resetUrls.GetValueOrDefault(email);
    public bool WasWelcomed(string email) => _welcomed.Contains(email, StringComparer.OrdinalIgnoreCase);

    public Task<bool> SendVerificationCodeAsync(string email, string code, string? language = null)
    {
        _codes[email] = code;
        return Task.FromResult(true);
    }

    public Task<bool> SendPasswordResetAsync(string email, string firstName, string resetUrl, string? language = null)
    {
        _resetUrls[email] = resetUrl;
        return Task.FromResult(true);
    }

    public Task<bool> SendWelcomeEmailAsync(string email, string firstName, string? language = null)
    {
        _welcomed.Add(email);
        return Task.FromResult(true);
    }
}
