using FishDex.Domain.DTOs.Species;
using FishLover.Shared.Common;

namespace FishDex.Domain.Services.Interfaces;

public interface ISpeciesService
{
    Task<PagedResult<SpeciesDto>> GetSpeciesAsync(GetSpeciesRequest request, CancellationToken ct = default);
    Task<SpeciesDto?> GetBySpecCodeAsync(int specCode, CancellationToken ct = default);
    Task<IReadOnlyList<FamilyDto>> GetFamiliesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<GenusDto>> GetGenusByFamilyAsync(Guid famId, CancellationToken ct = default);
}
