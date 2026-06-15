using AquaHome.EFCore.Entity;
using AquaHome.EFCore.Repository.Base;

namespace AquaHome.EFCore.Repository.Interface;

public interface IAquariumRepository : IGenericRepository<Aquarium>
{
    Task<IReadOnlyList<Aquarium>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task<Aquarium?> GetByIdAndUserAsync(Guid id, Guid userId, CancellationToken ct = default);

    Task<AquariumFish?> GetFishEntryAsync(Guid aquariumId, int specCode, CancellationToken ct = default);
    Task AddFishAsync(AquariumFish fish, CancellationToken ct = default);
    Task RemoveFishAsync(AquariumFish fish, CancellationToken ct = default);

    Task<IReadOnlyList<AquariumFish>> GetFishListAsync(Guid aquariumId, CancellationToken ct = default);
}
