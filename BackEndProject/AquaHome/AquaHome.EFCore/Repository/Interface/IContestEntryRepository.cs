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
    Task<long> SumStagingVideoBytesAsync(CancellationToken ct = default);
    Task AddAsync(ContestEntry entry, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
