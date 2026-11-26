using AquaHome.Common.Enums;
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

    public async Task<IReadOnlyList<ContestEntry>> GetByStatusAsync(ContestEntryStatus status, CancellationToken ct = default)
        => await db.ContestEntries
            .Where(e => e.Status == (int)status)
            .OrderBy(e => e.SubmittedAt)
            .ToListAsync(ct);

    // Sync viewCount mỗi 6h — chỉ entries thuộc contest đang Active
    public async Task<IReadOnlyList<ContestEntry>> GetByActiveContestsAsync(CancellationToken ct = default)
        => await db.ContestEntries
            .Where(e => e.Contest.Status == (int)ContestStatus.Active && e.YouTubeVideoId != null)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ContestEntry>> GetByUserAndContestAsync(
        Guid userId, Guid contestId, CancellationToken ct = default)
        => await db.ContestEntries
            .Where(e => e.UserId == userId && e.ContestId == contestId)
            .OrderByDescending(e => e.SubmittedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ContestEntry>> GetUploadQueueAsync(CancellationToken ct = default)
        => await db.ContestEntries
            .Include(e => e.Contest)
            .Where(e => e.Status == (int)ContestEntryStatus.Queued
                        && e.VideoR2Key != null
                        && e.VideoVerifiedAt != null)
            .OrderBy(e => e.VideoVerifiedAt)
            .ToListAsync(ct);

    public async Task<bool> TryClaimForCheckAsync(Guid entryId, CancellationToken ct = default)
        => await db.ContestEntries
            .Where(e => e.Id == entryId && e.Status == (int)ContestEntryStatus.Pending)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.Status, (int)ContestEntryStatus.Queued)
                .SetProperty(e => e.VideoVerifiedAt, (DateTime?)null), ct) == 1;

    public async Task<IReadOnlyList<ContestEntry>> GetWithStagingVideoAsync(DateTime submittedBefore, CancellationToken ct = default)
        => await db.ContestEntries.AsNoTracking()
            .Where(e => e.VideoR2Key != null && e.SubmittedAt < submittedBefore)
            .ToListAsync(ct);

    public async Task<bool> TryExpireStagingAsync(
        Guid entryId, ContestEntryStatus expected, string reason, CancellationToken ct = default)
        => await db.ContestEntries
            .Where(e => e.Id == entryId && e.Status == (int)expected)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.Status, (int)ContestEntryStatus.Failed)
                .SetProperty(e => e.RejectionReason, reason)
                .SetProperty(e => e.VideoR2Key, (string?)null), ct) == 1;

    // R2 storage guard — file đã HEAD-verify + bài mới submit (đang upload dở) mới tính
    public Task<long> SumStagingVideoBytesAsync(DateTime unverifiedSubmittedAfter, CancellationToken ct = default)
        => db.ContestEntries
            .Where(e => e.VideoR2Key != null
                        && (e.VideoVerifiedAt != null || e.SubmittedAt > unverifiedSubmittedAfter))
            .SumAsync(e => e.VideoSizeBytes ?? 0, ct);

    public async Task AddAsync(ContestEntry entry, CancellationToken ct = default)
        => await db.ContestEntries.AddAsync(entry, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
