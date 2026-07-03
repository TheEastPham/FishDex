using AquaHome.Common.Enums;

namespace AquaHome.Domain.Exceptions;

/// <summary>Ném khi user vượt giới hạn quota của role — API filter map thành HTTP 429.</summary>
public class QuotaExceededException(QuotaType quotaType, int limit, string role)
    : Exception($"Quota exceeded: {quotaType} (limit {limit} for role {role}).")
{
    public QuotaType QuotaType { get; } = quotaType;
    public int Limit { get; } = limit;
    public string Role { get; } = role;
}
