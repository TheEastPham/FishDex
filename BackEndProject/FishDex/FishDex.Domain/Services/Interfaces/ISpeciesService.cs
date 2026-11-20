using FishDex.Domain.DTOs.Species;

namespace FishDex.Domain.Services.Interfaces;

/// <summary>Lookup danh mục. Hồ sơ loài nằm ở <see cref="SpeciesProfiles.ISpeciesProfile"/>.</summary>
public interface ISpeciesService
{
    Task<IReadOnlyList<FamilyDto>> GetFamiliesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<GenusDto>> GetGenusByFamilyAsync(Guid famId, CancellationToken ct = default);
    Task<IReadOnlyList<LanguageCountDto>> GetTopLanguagesAsync(CancellationToken ct = default);
}
