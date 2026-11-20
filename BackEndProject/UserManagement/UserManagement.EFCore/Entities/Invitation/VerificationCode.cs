namespace UserManagement.EFCore.Entities.Invitation;

public class VerificationCode
{
    public Guid? InvitationId { get; set; }
    public string Code { get; set; } = null!;
    public DateTime CreatedAt  { get; set; }

    private int _attempts;

    /// <summary>
    /// Ghi nhận 1 lượt dùng mã, trả về tổng số lượt. Entry nằm trong MemoryCache (cùng 1 object cho mọi
    /// request) nên phải tăng atomic — request song song không được đếm hụt.
    /// </summary>
    public int CountAttempt() => Interlocked.Increment(ref _attempts);
}
