namespace AquaHome.EFCore.Repository.Interface;

public interface IAquariumSnapshotLikeRepository
{
    Task<bool> ExistsAsync(Guid snapshotId, Guid userId, CancellationToken ct = default);
    Task AddAsync(Guid snapshotId, Guid userId, CancellationToken ct = default);
    Task<bool> RemoveAsync(Guid snapshotId, Guid userId, CancellationToken ct = default);
}
