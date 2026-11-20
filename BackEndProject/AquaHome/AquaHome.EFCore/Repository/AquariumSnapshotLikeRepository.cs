using AquaHome.EFCore.Data;
using AquaHome.EFCore.Entity;
using AquaHome.EFCore.Repository.Interface;
using Microsoft.EntityFrameworkCore;

namespace AquaHome.EFCore.Repository;

public class AquariumSnapshotLikeRepository(AquaHomeDbContext db) : IAquariumSnapshotLikeRepository
{
    public Task<bool> ExistsAsync(Guid snapshotId, Guid userId, CancellationToken ct = default)
        => db.AquariumSnapshotLikes.AnyAsync(l => l.SnapshotId == snapshotId && l.UserId == userId, ct);

    public async Task AddAsync(Guid snapshotId, Guid userId, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        db.AquariumSnapshotLikes.Add(new AquariumSnapshotLike
        {
            Id = Guid.NewGuid(),
            SnapshotId = snapshotId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
        });

        var snapshot = await db.AquariumSnapshots.FirstAsync(s => s.Id == snapshotId, ct);
        snapshot.LikeCount++;

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task<bool> RemoveAsync(Guid snapshotId, Guid userId, CancellationToken ct = default)
    {
        var like = await db.AquariumSnapshotLikes
            .FirstOrDefaultAsync(l => l.SnapshotId == snapshotId && l.UserId == userId, ct);
        if (like is null) return false;

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        db.AquariumSnapshotLikes.Remove(like);

        var snapshot = await db.AquariumSnapshots.FirstAsync(s => s.Id == snapshotId, ct);
        if (snapshot.LikeCount > 0) snapshot.LikeCount--;

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return true;
    }
}
