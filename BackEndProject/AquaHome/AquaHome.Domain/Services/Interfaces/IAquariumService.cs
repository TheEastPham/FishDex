using AquaHome.Domain.DTOs;

namespace AquaHome.Domain.Services.Interfaces;

public interface IAquariumService
{
    Task<IReadOnlyList<AquariumDto>> GetMyAquariumsAsync(CancellationToken ct = default);
    Task<AquariumDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<AquariumDto> CreateAsync(CreateAquariumRequest request, CancellationToken ct = default);
    Task<AquariumDto?> UpdateAsync(Guid id, UpdateAquariumRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);

    Task<bool> AddFishAsync(Guid aquariumId, int specCode, int quantity, CancellationToken ct = default);
    Task<bool> RemoveFishAsync(Guid aquariumId, int specCode, CancellationToken ct = default);
}
