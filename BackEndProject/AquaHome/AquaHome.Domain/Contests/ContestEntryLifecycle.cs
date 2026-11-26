using AquaHome.Common.Enums;
using AquaHome.Domain.Exceptions;
using AquaHome.EFCore.Entity;
using FishLover.Storage;

namespace AquaHome.Domain.Contests;

/// <summary>
/// Vòng đời bài dự thi — nơi duy nhất đổi <see cref="ContestEntry.Status"/>. Thuần: không gọi R2, YouTube hay DB.
/// Người gọi lấy dữ liệu, gọi dịch vụ ngoài, rồi báo kết quả vào đây; bước nào bỏ video staging thì trả key của nó
/// để người gọi xoá trên R2 sau khi đã lưu.
/// <code>
/// Pending ──xác nhận──► Queued ──Worker đăng──► UploadedDraft ──duyệt──► Published
///    ▲   R2 lỗi lúc kiểm  │  file thiếu/lệch, YouTube hỏng, quá hạn           │
///    └────────────────────┘──────────────────► Failed     UploadedDraft/Published ──từ chối──► Rejected
/// </code>
/// Bước sai trạng thái do code gọi nhầm ném <see cref="InvalidOperationException"/>; thao tác của admin không hợp lệ
/// ném <see cref="ContestValidationException"/> (API trả 422 kèm lý do).
/// </summary>
public static class ContestEntryLifecycle
{
    public const string VideoNotFoundReason = "Không tìm thấy video đã tải lên. Bạn hãy nộp lại bài.";
    public const string VideoSizeMismatchReason = "Video tải lên không khớp dung lượng đã khai. Bạn hãy nộp lại bài.";
    public const string PostFailedReason = "Không đăng được video lên YouTube. Bạn hãy nộp lại bài.";
    public const string AbandonedReason = "Video chưa được xác nhận sau khi tải lên nên đã bị xoá. Bạn hãy nộp lại bài.";
    public const string ExpiredReason = "Quá 3 ngày chưa đăng được lên YouTube nên video đã bị xoá. Bạn hãy nộp lại bài.";

    /// <summary>Bài chưa xác nhận file được giữ video staging bấy lâu — upload rồi bỏ thì dọn sớm.</summary>
    public static readonly TimeSpan UnconfirmedStagingLifetime = TimeSpan.FromDays(1);

    /// <summary>
    /// Bài Pending đã kiểm file từ luồng cũ (bị hoãn vì hết lượt, được hẹn quay lại bấm "Xác nhận lại"): giữ đúng hạn
    /// đã hứa. Bài trong hàng chờ đã kiểm file thì không hết hạn — Worker sẽ đăng khi có lượt.
    /// </summary>
    public static readonly TimeSpan ConfirmedPendingStagingLifetime = TimeSpan.FromDays(3);

    // ── Câu hỏi dùng chung ────────────────────────────────────────────────

    /// <summary>Bài còn hiệu lực: tính vào trần số bài của một người và chặn nộp lại cùng bể.</summary>
    public static bool IsActive(ContestEntry entry)
        => Status(entry) is not (ContestEntryStatus.Rejected or ContestEntryStatus.Failed);

    /// <summary>Bài còn chờ hệ thống hoặc ban tổ chức — cuộc thi chưa kết thúc được khi còn bài loại này.</summary>
    public static bool AwaitsDecision(ContestEntry entry)
        => Status(entry) is ContestEntryStatus.Queued or ContestEntryStatus.UploadedDraft;

    /// <summary>Người dự thi bấm xác nhận (lần đầu hoặc lại) được không.</summary>
    public static bool CanConfirm(ContestEntry entry)
        => Status(entry) == ContestEntryStatus.Pending && !string.IsNullOrEmpty(entry.VideoR2Key);

    // ── Người dự thi xác nhận file ────────────────────────────────────────

    /// <summary>Pending → Queued khi bắt đầu kiểm file. Phản chiếu câu UPDATE <c>TryClaimForCheckAsync</c> vào entity đang track.</summary>
    public static void StartCheck(ContestEntry entry)
    {
        Require(entry, ContestEntryStatus.Pending);
        entry.Status = (int)ContestEntryStatus.Queued;
        entry.VideoVerifiedAt = null;
    }

    /// <summary>
    /// Kết quả kiểm file. Đạt: ghi mốc xác nhận, bài ở lại hàng chờ. Không đạt: Failed — verifier đã xoá object quá cỡ
    /// hoặc lệch, còn chưa có file thì không có gì để xoá.
    /// </summary>
    public static void FileChecked(ContestEntry entry, UploadVerdict verdict, DateTime now)
    {
        Require(entry, ContestEntryStatus.Queued);
        if (verdict.IsOk)
        {
            entry.VideoVerifiedAt = now;
            return;
        }

        Fail(entry, verdict.Kind == UploadVerdictKind.NotUploaded ? VideoNotFoundReason : VideoSizeMismatchReason);
    }

    /// <summary>Storage trục trặc lúc kiểm — lỗi hệ thống chứ không phải lỗi bài: trả về Pending để người dự thi thử lại.</summary>
    public static void CheckUnavailable(ContestEntry entry)
    {
        Require(entry, ContestEntryStatus.Queued);
        entry.Status = (int)ContestEntryStatus.Pending;
    }

    // ── Worker đăng YouTube ───────────────────────────────────────────────

    /// <summary>Bài trong hàng chờ có được đăng ngay không: cuộc thi phải đang mở (admin tạm đóng thì bài cứ chờ).</summary>
    public static bool IsReadyToPost(ContestEntry entry, Contest contest)
        => Status(entry) == ContestEntryStatus.Queued
           && entry.VideoVerifiedAt is not null
           && !string.IsNullOrEmpty(entry.VideoR2Key)
           && contest.Status == (int)ContestStatus.Active;

    /// <summary>YouTube đã nhận video: chờ duyệt, bản staging hết vai trò. Trả key staging cần xoá.</summary>
    public static string? Posted(ContestEntry entry, string youTubeVideoId)
    {
        Require(entry, ContestEntryStatus.Queued);
        var staging = entry.VideoR2Key;
        entry.YouTubeVideoId = youTubeVideoId;
        entry.Status = (int)ContestEntryStatus.UploadedDraft;
        entry.VideoR2Key = null;
        return staging;
    }

    /// <summary>YouTube không nhận video (không phải hết quota — trường hợp đó bài cứ ở hàng chờ). Trả key staging cần xoá.</summary>
    public static string? PostFailed(ContestEntry entry)
    {
        Require(entry, ContestEntryStatus.Queued);
        var staging = entry.VideoR2Key;
        Fail(entry, PostFailedReason);
        return staging;
    }

    // ── Ban tổ chức ───────────────────────────────────────────────────────

    /// <summary>Kiểm trước khi gọi YouTube công khai video: chỉ bài chờ duyệt, và cuộc thi chưa kết thúc.</summary>
    public static void EnsureCanApprove(ContestEntry entry, Contest? contest)
    {
        EnsureContestNotEnded(contest);
        if (Status(entry) != ContestEntryStatus.UploadedDraft || entry.YouTubeVideoId is null)
            throw new ContestValidationException("Chỉ duyệt được bài đang chờ duyệt.");
    }

    public static void Publish(ContestEntry entry)
    {
        Require(entry, ContestEntryStatus.UploadedDraft);
        entry.Status = (int)ContestEntryStatus.Published;
    }

    /// <summary>Kiểm trước khi xoá video trên YouTube: chỉ bài đã lên YouTube (chờ duyệt hoặc đã công khai), cuộc thi chưa kết thúc.</summary>
    public static void EnsureCanReject(ContestEntry entry, Contest? contest)
    {
        EnsureContestNotEnded(contest);
        if (Status(entry) is not (ContestEntryStatus.UploadedDraft or ContestEntryStatus.Published))
            throw new ContestValidationException("Chỉ từ chối được bài đã lên YouTube.");
    }

    public static void Reject(ContestEntry entry, string reason)
    {
        if (Status(entry) is not (ContestEntryStatus.UploadedDraft or ContestEntryStatus.Published))
            throw new InvalidOperationException($"Bài {entry.Id} ở trạng thái {Status(entry)} không từ chối được.");
        entry.Status = (int)ContestEntryStatus.Rejected;
        entry.RejectionReason = reason.Trim();
        entry.VideoR2Key = null;
    }

    /// <summary>Cuộc thi chỉ kết thúc (trao giải hay đổi trạng thái) khi không còn bài nào chờ đăng hoặc chờ duyệt.</summary>
    public static void EnsureContestCanEnd(IEnumerable<ContestEntry> entriesOfContest)
    {
        var waiting = entriesOfContest.Count(AwaitsDecision);
        if (waiting > 0)
            throw new ContestValidationException(
                $"Còn {waiting} bài đang chờ đăng lên YouTube hoặc chờ duyệt. Đợi bài lên YouTube, duyệt hoặc từ chối hết rồi mới kết thúc cuộc thi.");
    }

    // ── Job dọn staging ───────────────────────────────────────────────────

    /// <summary>
    /// Lý do bỏ bài nếu video staging của nó đã nằm quá lâu, null nếu chưa tới hạn hoặc bài không giữ staging.
    /// Bài trong hàng chờ đã kiểm file không bao giờ hết hạn ở đây.
    /// </summary>
    public static string? StagingExpiry(ContestEntry entry, DateTime now)
    {
        if (string.IsNullOrEmpty(entry.VideoR2Key)) return null;

        var age = now - entry.SubmittedAt;
        var verified = entry.VideoVerifiedAt is not null;

        return Status(entry) switch
        {
            ContestEntryStatus.Pending when verified => age > ConfirmedPendingStagingLifetime ? ExpiredReason : null,
            ContestEntryStatus.Pending or ContestEntryStatus.Queued when !verified
                => age > UnconfirmedStagingLifetime ? AbandonedReason : null,
            _ => null,
        };
    }

    // ──────────────────────────────────────────────────────────────────────

    private static ContestEntryStatus Status(ContestEntry entry) => (ContestEntryStatus)entry.Status;

    private static void Fail(ContestEntry entry, string reason)
    {
        entry.Status = (int)ContestEntryStatus.Failed;
        entry.RejectionReason = reason;
        entry.VideoR2Key = null;
    }

    private static void Require(ContestEntry entry, ContestEntryStatus expected)
    {
        if (Status(entry) != expected)
            throw new InvalidOperationException($"Bài {entry.Id} đang ở {Status(entry)}, bước này cần {expected}.");
    }

    private static void EnsureContestNotEnded(Contest? contest)
    {
        if (contest?.Status == (int)ContestStatus.Ended)
            throw new ContestValidationException("Cuộc thi đã kết thúc, không duyệt hay từ chối thêm bài.");
    }
}
