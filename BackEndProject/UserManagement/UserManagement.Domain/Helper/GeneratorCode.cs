using System.Text;

namespace UserManagement.Domain.Helper;

public static class GeneratorCode
{
    private const string AllowedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public static string GenerateRandomCode(string source, int length = 6)
    {
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Email cannot be null or empty", nameof(source));
        
        if (length < 4 || length > 10)
            throw new ArgumentException("Token length must be between 4 and 10 characters", nameof(length));

        // Use source as seed for consistent generation (optional - for testing purposes)
        var emailHash = GetSourceHash(source);
        var random = new Random(emailHash);
        
        // Generate token with mixed alphanumeric characters
        var token = new StringBuilder();
        for (var i = 0; i < length; i++)
        {
            // Add some randomness based on current time and email
            var timeComponent = (int)(DateTime.UtcNow.Ticks % AllowedChars.Length);
            var index = (random.Next(AllowedChars.Length) + timeComponent + i) % AllowedChars.Length;
            token.Append(AllowedChars[index]);
        }
        
        return token.ToString();
    }
    
    private static int GetSourceHash(string source)
    {
        var hash = source.ToLowerInvariant().Aggregate(0, (current, c) => (current * 31 + c) % int.MaxValue);
        return Math.Abs(hash);
    }
}