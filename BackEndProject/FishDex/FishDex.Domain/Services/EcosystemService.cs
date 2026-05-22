using FishDex.Domain.DTOs.Ecosystem;
using FishDex.Domain.Services.Interfaces;

namespace FishDex.Domain.Services;

// Ecosystem entity chưa có fields — placeholder để mở rộng sau
public class EcosystemService : IEcosystemService
{
    public Task<IReadOnlyList<EcosystemDto>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<EcosystemDto>>([]);

    public Task<EcosystemDto?> GetByIdAsync(int id, CancellationToken ct = default)
        => Task.FromResult<EcosystemDto?>(null);
}
