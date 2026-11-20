using AquaHome.Domain.DTOs;

namespace AquaHome.Domain.Services.Interfaces;

public interface IReminderService
{
    Task<IReadOnlyList<ReminderDto>> GetByAquariumAsync(Guid aquariumId, CancellationToken ct = default);
    Task<IReadOnlyList<UserReminderDto>> GetAllByUserAsync(CancellationToken ct = default);
    Task<ReminderDto> CreateAsync(Guid aquariumId, CreateReminderRequest request, CancellationToken ct = default);
    Task<CompleteReminderResponse> CompleteAsync(Guid aquariumId, Guid reminderId, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid aquariumId, Guid reminderId, CancellationToken ct = default);
}
