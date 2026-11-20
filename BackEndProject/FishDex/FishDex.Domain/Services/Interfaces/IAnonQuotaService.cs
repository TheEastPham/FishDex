namespace FishDex.Domain.Services.Interfaces;

/// <summary>Kết quả một lần "tiêu" hạn mức. <paramref name="Used"/> đã tính cả lượt vừa trừ.</summary>
public record AnonQuotaResult(bool Allowed, int Used, int Limit, int ResetsInSeconds)
{
    public int Remaining => Math.Max(0, Limit - Used);
}

public interface IAnonQuotaService
{
    /// <summary>
    /// Ghi nhận khách xem một loài. Xem lại loài đã xem trong ngày thì không trừ thêm lượt.
    /// Redis hỏng thì fail-open (cho qua) — hạn mức không đáng để làm sập trang tra cứu.
    /// </summary>
    Task<AnonQuotaResult> TryConsumeAsync(string visitorId, string clientIp, int specCode, CancellationToken ct = default);
}
