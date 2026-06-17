using AquaHome.EFCore.Entity;

namespace AquaHome.EFCore.Repository.Interface;

public interface IAquariumMediaRepository
{
    Task<IReadOnlyList<AquariumMedia>> GetByAquariumAsync(Guid aquariumId, CancellationToken ct = default);
    Task<int>                          CountByAquariumAsync(Guid aquariumId, CancellationToken ct = default);
    Task<AquariumMedia?>               GetByIdAsync(Guid id, CancellationToken ct = default);
    Task                               AddAsync(AquariumMedia media, CancellationToken ct = default);
    void                               Remove(AquariumMedia media);
    Task                               SaveChangesAsync(CancellationToken ct = default);
}
