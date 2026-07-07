using AquaHome.EFCore.Entity;

namespace AquaHome.EFCore.Repository.Interface;

public interface IAquariumSnapshotRepository
{
    Task<AquariumSnapshot?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<AquariumSnapshot?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<IReadOnlyList<AquariumSnapshot>> GetActiveByAquariumAsync(Guid aquariumId, CancellationToken ct = default);
    Task<IReadOnlyList<AquariumSnapshot>> GetActiveByUserAsync(Guid userId, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default);

    Task<(IReadOnlyList<AquariumSnapshot> Items, int TotalCount)> GetGalleryAsync(
        int? waterType, int? style, string? contest, string sort, int page, int pageSize, CancellationToken ct = default);

    Task AddAsync(AquariumSnapshot snapshot, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
