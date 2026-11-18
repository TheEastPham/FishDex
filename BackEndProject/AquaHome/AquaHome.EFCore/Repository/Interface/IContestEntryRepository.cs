using AquaHome.EFCore.Entity;

namespace AquaHome.EFCore.Repository.Interface;

public interface IContestEntryRepository
{
    Task<ContestEntry?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ContestEntry>> GetByContestAsync(Guid contestId, CancellationToken ct = default);
    Task<IReadOnlyList<ContestEntry>> GetByStatusAsync(int status, CancellationToken ct = default);
    Task<IReadOnlyList<ContestEntry>> GetByActiveContestsAsync(CancellationToken ct = default);

    /// <summary>Bài dự thi của 1 user trong 1 contest (mọi trạng thái) — cho trang "bài dự thi của tôi".</summary>
    Task<IReadOnlyList<ContestEntry>> GetByUserAndContestAsync(Guid userId, Guid contestId, CancellationToken ct = default);

    /// <summary>
    /// User này đã nộp snapshot này cho contest này chưa (bỏ qua bài đã bị từ chối để cho phép nộp lại)?
    /// Chặn nộp trùng — mỗi lần upload YouTube tốn 1.600 quota units trên tổng 10.000/ngày.
    /// </summary>
    Task<bool> HasActiveEntryForSnapshotAsync(Guid userId, Guid contestId, Guid snapshotId, CancellationToken ct = default);

    /// <summary>Số bài của user trong contest, không tính bài đã bị từ chối — cho trần số bài / user / contest.</summary>
    Task<int> CountActiveEntriesAsync(Guid userId, Guid contestId, CancellationToken ct = default);

    /// <summary>
    /// Chuyển Pending → Validating bằng 1 câu UPDATE có điều kiện. Trả false nếu bài không còn Pending
    /// (đang có request confirm khác xử lý). Không cập nhật entity đang được track.
    /// </summary>
    Task<bool> TryClaimForUploadAsync(Guid entryId, CancellationToken ct = default);

    /// <summary>Bài Pending/Validating còn video trên R2 staging, nộp trước mốc này — cho job dọn staging.</summary>
    Task<IReadOnlyList<ContestEntry>> GetStaleStagingAsync(DateTime submittedBefore, CancellationToken ct = default);

    /// <summary>
    /// Chuyển Rejected (kèm lý do) + bỏ VideoR2Key bằng 1 câu UPDATE, chỉ khi bài vẫn đang ở <paramref name="expectedStatus"/>.
    /// False nếu bài vừa đổi trạng thái (user đang bấm xác nhận lại) — khi đó không được xoá video.
    /// </summary>
    Task<bool> TryExpireStagingAsync(Guid entryId, int expectedStatus, string reason, CancellationToken ct = default);
    Task<long> SumStagingVideoBytesAsync(CancellationToken ct = default);
    Task AddAsync(ContestEntry entry, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
