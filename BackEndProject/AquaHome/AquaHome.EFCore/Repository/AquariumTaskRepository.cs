using AquaHome.EFCore.Data;
using AquaHome.EFCore.Entity;
using AquaHome.EFCore.Repository.Interface;
using Microsoft.EntityFrameworkCore;

namespace AquaHome.EFCore.Repository;

public class AquariumTaskRepository(AquaHomeDbContext context) : IAquariumTaskRepository
{
    public async Task<IReadOnlyList<AquariumTask>> GetByAquariumAsync(Guid aquariumId, CancellationToken ct = default)
        => await context.AquariumTasks
            .Where(t => t.AquariumId == aquariumId)
            .OrderBy(t => t.DueAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AquariumTask>> GetByUserAsync(Guid userId, CancellationToken ct = default)
        => await context.AquariumTasks
            .Include(t => t.Aquarium)
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.IsCompleted)
            .ThenBy(t => t.DueAt)
            .ToListAsync(ct);

    public async Task<AquariumTask?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.AquariumTasks.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IReadOnlyList<AquariumTask>> GetDueUnremindedAsync(DateTime cutoff, CancellationToken ct = default)
        => await context.AquariumTasks
            .Include(t => t.Aquarium)
            .Where(t => !t.IsCompleted && !t.Reminded && t.DueAt <= cutoff)
            .ToListAsync(ct);

    public async Task<AquariumTask> AddAsync(AquariumTask task, CancellationToken ct = default)
    {
        context.AquariumTasks.Add(task);
        await context.SaveChangesAsync(ct);
        return task;
    }

    public async Task UpdateAsync(AquariumTask task, CancellationToken ct = default)
    {
        context.AquariumTasks.Update(task);
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(AquariumTask task, CancellationToken ct = default)
    {
        context.AquariumTasks.Remove(task);
        await context.SaveChangesAsync(ct);
    }

    public async Task MarkRemindedAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        await context.AquariumTasks
            .Where(t => ids.Contains(t.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.Reminded, true), ct);
    }
}
