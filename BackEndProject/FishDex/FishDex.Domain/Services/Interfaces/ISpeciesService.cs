using FishDex.Domain.DTOs.Species;
using FishLover.Shared.Common;

namespace FishDex.Domain.Services.Interfaces;

public interface ISpeciesService
{
    Task<PagedResult<SpeciesDto>> GetSpeciesAsync(GetSpeciesRequest request, CancellationToken ct = default);
    Task<SpeciesDto?> GetBySpecCodeAsync(int specCode, CancellationToken ct = default);
    Task<IReadOnlyList<FamilyDto>> GetFamiliesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<GenusDto>> GetGenusByFamilyAsync(Guid famId, CancellationToken ct = default);
    Task<IReadOnlyList<CommonNameDto>> GetCommonNamesBySpecCodeAsync(int specCode, CancellationToken ct = default);
    Task<IReadOnlyList<CommonNameDto>> SearchCommonNamesAsync(string term, string? language = null, CancellationToken ct = default);
    Task<PagedResult<SpeciesSearchResultDto>> SearchSpeciesAsync(GetSpeciesSearchRequest request, CancellationToken ct = default);
    Task<SpeciesDetailDto?> GetDetailAsync(int specCode, string? language = null, CancellationToken ct = default);
    Task<IReadOnlyList<LanguageCountDto>> GetTopLanguagesAsync(CancellationToken ct = default);
}
