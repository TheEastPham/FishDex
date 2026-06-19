using FishDex.Domain.DTOs.Ecologies;

namespace FishDex.Domain.Services.Interfaces;

public interface IEcologyService
{
    Task<EcologyDto?> GetBySpecCodeAsync(int specCode, CancellationToken ct = default);
    Task<FeedingAndDietDto?> GetFeedingAsync(int ecologyId, CancellationToken ct = default);
    Task<HabitatZoneDto?> GetHabitatZoneAsync(int ecologyId, CancellationToken ct = default);
    Task<AssociationsDto?> GetAssociationsAsync(int ecologyId, CancellationToken ct = default);
    Task<SubstrateDto?> GetSubstrateAsync(int ecologyId, CancellationToken ct = default);
    Task<SpecialHabitatDto?> GetSpecialHabitatAsync(int ecologyId, CancellationToken ct = default);
}
