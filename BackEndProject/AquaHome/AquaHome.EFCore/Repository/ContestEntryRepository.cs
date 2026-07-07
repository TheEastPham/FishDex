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
