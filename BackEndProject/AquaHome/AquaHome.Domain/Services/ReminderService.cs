using AquaHome.Common.Enums;
using AquaHome.Domain.DTOs;
using AquaHome.Domain.Services.Interfaces;
using AquaHome.EFCore.Entity;
using AquaHome.EFCore.Repository.Interface;
using FishLover.Shared.Services;

namespace AquaHome.Domain.Services;

public class ReminderService(
    IAquariumTaskRepository taskRepo,
    IAquariumRepository aquariumRepo,
    ICurrentUserSession currentUser) : IReminderService
{
    public async Task<IReadOnlyList<ReminderDto>> GetByAquariumAsync(Guid aquariumId, CancellationToken ct = default)
    {
        // Ownership check
        var aquarium = await aquariumRepo.GetByIdAndUserAsync(aquariumId, currentUser.UserId, ct);
        if (aquarium is null) return [];

        var tasks = await taskRepo.GetByAquariumAsync(aquariumId, ct);
        return tasks.Select(ToDto).ToList();
    }

    public async Task<ReminderDto> CreateAsync(Guid aquariumId, CreateReminderRequest request, CancellationToken ct = default)
    {
        var aquarium = await aquariumRepo.GetByIdAndUserAsync(aquariumId, currentUser.UserId, ct)
            ?? throw new KeyNotFoundException("Aquarium not found");

        var task = new AquariumTask
        {
            UserId      = currentUser.UserId,
            AquariumId  = aquarium.Id,
            AquaTaskType    = request.AquaTaskType,
            DueAt       = request.DueAt.ToUniversalTime(),
            IntervalDays = request.IntervalDays,
        };

        await taskRepo.AddAsync(task, ct);
        return ToDto(task);
    }

    public async Task<CompleteReminderResponse> CompleteAsync(Guid aquariumId, Guid reminderId, CancellationToken ct = default)
    {
        var task = await GetOwnedTaskAsync(aquariumId, reminderId, ct)
            ?? throw new KeyNotFoundException("Reminder not found");

        task.IsCompleted = true;
        task.CompletedAt = DateTime.UtcNow;
        await taskRepo.UpdateAsync(task, ct);

        DateTime? suggestedNext = task.IntervalDays.HasValue
            ? task.CompletedAt!.Value.AddDays(task.IntervalDays.Value)
            : null;

        return new CompleteReminderResponse(task.Id, suggestedNext);
    }

    public async Task<bool> DeleteAsync(Guid aquariumId, Guid reminderId, CancellationToken ct = default)
    {
        var task = await GetOwnedTaskAsync(aquariumId, reminderId, ct);
        if (task is null) return false;

        await taskRepo.DeleteAsync(task, ct);
        return true;
    }

    private async Task<AquariumTask?> GetOwnedTaskAsync(Guid aquariumId, Guid reminderId, CancellationToken ct)
    {
        var task = await taskRepo.GetByIdAsync(reminderId, ct);
        if (task is null || task.AquariumId != aquariumId || task.UserId != currentUser.UserId)
            return null;
        return task;
    }

    public async Task<IReadOnlyList<UserReminderDto>> GetAllByUserAsync(CancellationToken ct = default)
    {
        var tasks = await taskRepo.GetByUserAsync(currentUser.UserId, ct);
        return tasks.Select(ToUserDto).ToList();
    }

    private static ReminderDto ToDto(AquariumTask t) => new(
        t.Id, t.AquariumId, t.AquaTaskType, t.DueAt, t.IntervalDays, t.IsCompleted, t.CompletedAt);

    private static UserReminderDto ToUserDto(AquariumTask t) => new(
        t.Id, t.AquariumId, t.Aquarium?.Name ?? string.Empty, t.AquaTaskType, t.DueAt, t.IntervalDays, t.IsCompleted, t.CompletedAt);
}
