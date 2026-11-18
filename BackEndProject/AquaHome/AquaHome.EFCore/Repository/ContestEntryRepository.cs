using AquaHome.EFCore.Data;
using AquaHome.EFCore.Entity;
using AquaHome.EFCore.Repository.Interface;
using Microsoft.EntityFrameworkCore;

namespace AquaHome.EFCore.Repository;

public class ContestEntryRepository(AquaHomeDbContext db) : IContestEntryRepository
{
    public Task<ContestEntry?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.ContestEntries.FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IReadOnlyList<ContestEntry>> GetByContestAsync(Guid contestId, CancellationToken ct = default)
        => await db.ContestEntries
            .Where(e => e.ContestId == contestId)
            .OrderByDescending(e => e.YouTubeViewCount)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ContestEntry>> GetByStatusAsync(int status, CancellationToken ct = default)
        => await db.ContestEntries
            .Where(e => e.Status == status)
            .OrderBy(e => e.SubmittedAt)
            .ToListAsync(ct);

    // Sync viewCount mỗi 6h — chỉ entries thuộc contest đang Active
    public async Task<IReadOnlyList<ContestEntry>> GetByActiveContestsAsync(CancellationToken ct = default)
        => await db.ContestEntries
            .Where(e => e.Contest.Status == 1 && e.YouTubeVideoId != null) // ContestStatus.Active
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ContestEntry>> GetByUserAndContestAsync(
        Guid userId, Guid contestId, CancellationToken ct = default)
        => await db.ContestEntries
            .Where(e => e.UserId == userId && e.ContestId == contestId)
            .OrderByDescending(e => e.SubmittedAt)
            .ToListAsync(ct);

    public Task<bool> HasActiveEntryForSnapshotAsync(
        Guid userId, Guid contestId, Guid snapshotId, CancellationToken ct = default)
        => db.ContestEntries.AnyAsync(
            e => e.UserId == userId
                 && e.ContestId == contestId
                 && e.AquariumSnapshotId == snapshotId
                 && e.Status != 4, // ContestEntryStatus.Rejected — bài trượt thì cho nộp lại
            ct);

    public Task<int> CountActiveEntriesAsync(Guid userId, Guid contestId, CancellationToken ct = default)
        => db.ContestEntries.CountAsync(
            e => e.UserId == userId
                 && e.ContestId == contestId
                 && e.Status != 4, // ContestEntryStatus.Rejected
            ct);

    public async Task<bool> TryClaimForUploadAsync(Guid entryId, CancellationToken ct = default)
        => await db.ContestEntries
            .Where(e => e.Id == entryId && e.Status == 0) // ContestEntryStatus.Pending
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.Status, 1), ct) == 1; // → Validating

    public async Task<IReadOnlyList<ContestEntry>> GetStaleStagingAsync(DateTime submittedBefore, CancellationToken ct = default)
        => await db.ContestEntries.AsNoTracking()
            .Where(e => (e.Status == 0 || e.Status == 1) // ContestEntryStatus.Pending / Validating
                        && e.VideoR2Key != null
                        && e.SubmittedAt < submittedBefore)
            .ToListAsync(ct);

    public async Task<bool> TryExpireStagingAsync(Guid entryId, int expectedStatus, string reason, CancellationToken ct = default)
        => await db.ContestEntries
            .Where(e => e.Id == entryId && e.Status == expectedStatus)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.Status, 4) // → ContestEntryStatus.Rejected
                .SetProperty(e => e.RejectionReason, reason)
                .SetProperty(e => e.VideoR2Key, (string?)null), ct) == 1;

    // R2 storage guard — tổng staging = SUM(VideoSizeBytes) WHERE VideoR2Key IS NOT NULL
    public Task<long> SumStagingVideoBytesAsync(CancellationToken ct = default)
        => db.ContestEntries
            .Where(e => e.VideoR2Key != null)
            .SumAsync(e => e.VideoSizeBytes ?? 0, ct);

    public async Task AddAsync(ContestEntry entry, CancellationToken ct = default)
        => await db.ContestEntries.AddAsync(entry, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
