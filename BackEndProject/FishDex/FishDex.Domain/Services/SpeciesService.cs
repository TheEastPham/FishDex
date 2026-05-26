using FishDex.Domain.DTOs.Species;
using FishDex.Domain.Mappings;
using FishDex.Domain.Services.Interfaces;
using FishDex.Domain.Settings;
using FishDex.EFCore.Repository.Interface;
using FishLover.Shared.Common;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace FishDex.Domain.Services;

public class SpeciesService(
    ISpeciesRepository speciesRepo,
    IFamiliesRepository familyRepo,
    IGenusRepository genusRepo,
    ICommonNameRepository commonNameRepo,
    IMemoryCache cache,
    IOptions<FishDexSettings> settings) : ISpeciesService
{
    private readonly int _languageTopCount = settings.Value.LanguageTopCount;

    public async Task<PagedResult<SpeciesDto>> GetSpeciesAsync(GetSpeciesRequest request, CancellationToken ct = default)
    {
        var all = await speciesRepo.FindAsync(s =>
            (request.GenusCode == null || s.GenusCode == request.GenusCode) &&
            (request.SearchTerm == null || s.SpeciesName.Contains(request.SearchTerm)));

        var list = all.ToList();
        var items = list
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => s.ToDto())
            .ToList();

        return new PagedResult<SpeciesDto>
        {
            Items      = items,
            TotalCount = list.Count,
            Page       = request.Page,
            PageSize   = request.PageSize
        };
    }

    public async Task<SpeciesDto?> GetBySpecCodeAsync(int specCode, CancellationToken ct = default)
    {
        var results = await speciesRepo.FindAsync(s => s.SpecCode == specCode);
        return results.FirstOrDefault()?.ToDto();
    }

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

    public async Task<IReadOnlyList<CommonNameDto>> GetCommonNamesBySpecCodeAsync(int specCode, CancellationToken ct = default)
    {
        var names = await commonNameRepo.FindAsync(c => c.SpecCode == specCode);
        return names.OrderByDescending(c => c.IsPreferred).ThenBy(c => c.Rank).Select(c => c.ToDto()).ToList();
    }

    public async Task<IReadOnlyList<CommonNameDto>> SearchCommonNamesAsync(string term, string? language = null, CancellationToken ct = default)
    {
        var names = await commonNameRepo.FindAsync(c =>
            c.ComName.Contains(term) &&
            (language == null || c.Language == language));
        return names.OrderByDescending(c => c.IsPreferred).ThenBy(c => c.Rank).Select(c => c.ToDto()).ToList();
    }

    public async Task<PagedResult<SpeciesSearchResultDto>> SearchSpeciesAsync(
        GetSpeciesSearchRequest request, CancellationToken ct = default)
    {
        var (items, total) = await speciesRepo.SearchWithCountAsync(
            request.Query, request.FamId, request.GenusCode, request.Language,
            request.Page, request.PageSize, ct);

        return new PagedResult<SpeciesSearchResultDto>
        {
            Items      = items.Select(s => s.ToSearchResultDto(request.Language)).ToList(),
            TotalCount = total,
            Page       = request.Page,
            PageSize   = request.PageSize
        };
    }

    public async Task<IReadOnlyList<LanguageCountDto>> GetTopLanguagesAsync(CancellationToken ct = default)
    {
        var cacheKey = $"languages:top:{_languageTopCount}";

        if (cache.TryGetValue(cacheKey, out IReadOnlyList<LanguageCountDto>? cached) && cached != null)
            return cached;

        var rows = await commonNameRepo.GetTopLanguagesAsync(_languageTopCount, ct);
        var result = rows.Select(r => new LanguageCountDto { Language = r.Language, Count = r.Count }).ToList();

        cache.Set(cacheKey, result, TimeSpan.FromHours(24));

        return result;
    }
}
