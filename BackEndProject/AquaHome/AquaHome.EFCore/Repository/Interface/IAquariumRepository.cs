using AquaHome.EFCore.Entity;
using AquaHome.EFCore.Repository.Base;

namespace AquaHome.EFCore.Repository.Interface;

public interface IAquariumRepository : IGenericRepository<Aquarium>
{
    Task<IReadOnlyList<Aquarium>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task<Aquarium?> GetByIdAndUserAsync(Guid id, Guid userId, CancellationToken ct = default);
}
