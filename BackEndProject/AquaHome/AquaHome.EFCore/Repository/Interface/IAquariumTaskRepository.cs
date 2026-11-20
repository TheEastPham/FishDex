using AquaHome.EFCore.Entity;

namespace AquaHome.EFCore.Repository.Interface;

public interface IAquariumTaskRepository
{
    Task<IReadOnlyList<AquariumTask>> GetByAquariumAsync(Guid aquariumId, CancellationToken ct = default);
    Task<IReadOnlyList<AquariumTask>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task<AquariumTask?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<AquariumTask>> GetDueUnremindedAsync(DateTime cutoff, CancellationToken ct = default);
    Task<AquariumTask> AddAsync(AquariumTask task, CancellationToken ct = default);
    Task UpdateAsync(AquariumTask task, CancellationToken ct = default);
    Task DeleteAsync(AquariumTask task, CancellationToken ct = default);
    Task MarkRemindedAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
}
