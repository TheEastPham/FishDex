using FishDex.Domain.DTOs.Ecosystem;

namespace FishDex.Domain.Services.Interfaces;

public interface IEcosystemService
{
    Task<IReadOnlyList<EcosystemRefDto>> GetAllEcosystemRefsAsync(CancellationToken ct = default);
    Task<EcosystemRefDto?> GetEcosystemRefByCodeAsync(int eCode, CancellationToken ct = default);
    Task<IReadOnlyList<EcosystemDto>> GetBySpecCodeAsync(int specCode, CancellationToken ct = default);
    Task<IReadOnlyList<EcosystemDto>> GetByECodeAsync(int eCode, CancellationToken ct = default);
}
