using FishDex.Domain.DTOs.Species;
using FishDex.Domain.Mappings;
using FishDex.Domain.Services.Interfaces;
using FishDex.Domain.Settings;
using FishDex.EFCore.Repository.Interface;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace FishDex.Domain.Services;

/// <summary>
/// Lookup danh mục (họ, chi, ngôn ngữ). Hồ sơ loài — detail, search, related, summaries — nằm ở
/// <see cref="SpeciesProfiles.ISpeciesProfile"/>.
/// </summary>
public class SpeciesService(
    IFamiliesRepository familyRepo,
    IGenusRepository genusRepo,
    ICommonNameRepository commonNameRepo,
    IMemoryCache cache,
    IOptions<FishDexSettings> settings) : ISpeciesService
{
    private readonly int _languageTopCount = settings.Value.LanguageTopCount;

    public async Task<IReadOnlyList<FamilyDto>> GetFamiliesAsync(CancellationToken ct = default)
    {
        var families = await familyRepo.FindAsync(x => x.Species.Any());
        return families.Select(f => f.ToDto()).ToList();
    }

    public async Task<IReadOnlyList<GenusDto>> GetGenusByFamilyAsync(Guid famId, CancellationToken ct = default)
    {
        var genera = await genusRepo.FindAsync(g => g.FamId == famId);
        return genera.Select(g => g.ToDto()).ToList();
    }

    public async Task<IReadOnlyList<LanguageCountDto>> GetTopLanguagesAsync(CancellationToken ct = default)
    {
        var cacheKey = $"languages:top:{_languageTopCount}";

        if (cache.TryGetValue(cacheKey, out IReadOnlyList<LanguageCountDto>? cached) && cached != null)
            return cached;

        var rows = await commonNameRepo.GetTopLanguagesAsync(_languageTopCount, ct);
        var result = rows.Select(r => new LanguageCountDto { Language = r.Language, Count = r.Count }).ToList();

        // AddMemoryCache đặt SizeLimit (Program.cs) → mọi entry BẮT BUỘC có Size, thiếu là ném InvalidOperationException.
        cache.Set(cacheKey, result, new MemoryCacheEntryOptions()
            .SetSize(1)
            .SetAbsoluteExpiration(TimeSpan.FromHours(24)));

        return result;
    }
}
