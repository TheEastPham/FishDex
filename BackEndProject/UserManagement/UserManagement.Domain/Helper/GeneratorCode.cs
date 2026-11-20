using System.Security.Cryptography;

namespace UserManagement.Domain.Helper;

public static class GeneratorCode
{
    private const string AllowedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    /// <summary>
    /// Mã xác thực gửi qua email. Sinh bằng CSPRNG — mã KHÔNG được suy ra từ email hay thời gian,
    /// nếu không ai biết thuật toán (repo public) cũng tính lại được mã của người khác.
    /// </summary>
    public static string GenerateRandomCode(int length = 6)
    {
        if (length < 4 || length > 10)
            throw new ArgumentException("Token length must be between 4 and 10 characters", nameof(length));

        return RandomNumberGenerator.GetString(AllowedChars, length);
    }
}
