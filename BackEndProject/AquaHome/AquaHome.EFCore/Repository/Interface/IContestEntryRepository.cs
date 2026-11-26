using AquaHome.Common.Enums;
using AquaHome.EFCore.Entity;

namespace AquaHome.EFCore.Repository.Interface;

/// <summary>
/// Truy vấn bài dự thi. Repository không quyết định trạng thái nào được chuyển sang trạng thái nào — luật đó nằm ở
/// <c>AquaHome.Domain.Contests.ContestEntryLifecycle</c>; ở đây chỉ có các câu đọc và hai câu UPDATE có điều kiện.
/// </summary>
public interface IContestEntryRepository
{
    Task<ContestEntry?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ContestEntry>> GetByContestAsync(Guid contestId, CancellationToken ct = default);
    Task<IReadOnlyList<ContestEntry>> GetByStatusAsync(ContestEntryStatus status, CancellationToken ct = default);
    Task<IReadOnlyList<ContestEntry>> GetByActiveContestsAsync(CancellationToken ct = default);

    /// <summary>Bài dự thi của 1 user trong 1 contest (mọi trạng thái) — trang "bài dự thi của tôi" và trần số bài.</summary>
    Task<IReadOnlyList<ContestEntry>> GetByUserAndContestAsync(Guid userId, Guid contestId, CancellationToken ct = default);

    /// <summary>
    /// Hàng chờ đăng YouTube: bài Queued đã kiểm file xong (có <c>VideoVerifiedAt</c>) và còn video staging, kèm
    /// <c>Contest</c>, bài xác nhận trước đứng trước. Bài đang được kiểm dở (chưa có <c>VideoVerifiedAt</c>) không lọt vào.
    /// </summary>
    Task<IReadOnlyList<ContestEntry>> GetUploadQueueAsync(CancellationToken ct = default);

    /// <summary>
    /// Pending → Queued bằng 1 câu UPDATE có điều kiện, xoá luôn <c>VideoVerifiedAt</c> cũ để Worker không lấy bài
    /// trong lúc đang kiểm lại file. False nếu bài không còn Pending (request xác nhận khác đã nhận).
    /// Không cập nhật entity đang được track.
    /// </summary>
    Task<bool> TryClaimForCheckAsync(Guid entryId, CancellationToken ct = default);

    /// <summary>Bài còn video staging, nộp trước mốc này — cho job dọn staging (không track).</summary>
    Task<IReadOnlyList<ContestEntry>> GetWithStagingVideoAsync(DateTime submittedBefore, CancellationToken ct = default);

    /// <summary>
    /// Chuyển Failed (kèm lý do) + bỏ <c>VideoR2Key</c> bằng 1 câu UPDATE, chỉ khi bài vẫn ở <paramref name="expected"/>.
    /// False nếu bài vừa đổi trạng thái (user đang xác nhận lại) — khi đó không được xoá video.
    /// </summary>
    Task<bool> TryExpireStagingAsync(Guid entryId, ContestEntryStatus expected, string reason, CancellationToken ct = default);

    /// <summary>
    /// Dung lượng staging R2 cho trần 9GB: bài đã HEAD-verify (file thật) + bài chưa verify nộp sau mốc này
    /// (đang upload dở). Bài khai dung lượng rồi bỏ đó không chiếm chỗ mãi — chỉ tính trong ít phút đầu.
    /// </summary>
    Task<long> SumStagingVideoBytesAsync(DateTime unverifiedSubmittedAfter, CancellationToken ct = default);

    Task AddAsync(ContestEntry entry, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
