using FishDex.Domain.DTOs.Ecosystem;

namespace FishDex.Domain.Services.Interfaces;

public interface IEcosystemService
{
    Task<IReadOnlyList<EcosystemDto>> GetAllAsync(CancellationToken ct = default);
    Task<EcosystemDto?> GetByIdAsync(int id, CancellationToken ct = default);
}
