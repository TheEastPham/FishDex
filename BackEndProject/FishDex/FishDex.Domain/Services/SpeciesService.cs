using System.IO;
using FishDex.Domain.DTOs.Ecologies;
using FishDex.Domain.DTOs.Species;
using FishDex.Domain.DTOs.Stocks;
using FishDex.Domain.Mappings;
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
    IStorageService storage,
    IEcologyService ecologyService,
    IStockService stockService,
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
        var language = NormalizeLanguage(request.Language);

        var (items, total) = await speciesRepo.SearchWithCountAsync(
            request.Query, request.FamId, request.GenusCode, language,
            request.Page, request.PageSize, ct);

        var mapped = new List<SpeciesSearchResultDto>(items.Count);
        foreach (var s in items)
        {
            var pic      = s.Pictures.FirstOrDefault(p => p.PicPreferred == true);
            var imageUrl = pic != null
                ? await storage.GetPresignedUrlAsync(pic.ObjectKey, ct)
                : null;
            mapped.Add(s.ToSearchResultDto(language, imageUrl));
        }

        return new PagedResult<SpeciesSearchResultDto>
        {
            Items      = mapped,
            TotalCount = total,
            Page       = request.Page,
            PageSize   = request.PageSize
        };
    }

    public async Task<SpeciesDetailDto?> GetDetailAsync(int specCode, string? language = null, CancellationToken ct = default)
    {
        language = NormalizeLanguage(language);

        var species = await speciesRepo.GetWithDetailsAsync(specCode, ct);
        if (species == null) return null;

        // Sequential — EF Core DbContext không thread-safe, không dùng Task.WhenAll với cùng scope
        var ecology    = await ecologyService.GetBySpecCodeAsync(specCode, ct);
        var stocks     = await stockService.GetBySpecCodeAsync(specCode, ct);
        var firstStock = stocks.FirstOrDefault();

        FeedingAndDietDto?      feeding      = ecology    != null ? await ecologyService.GetFeedingAsync(ecology.EcologyId, ct)       : null;
        HabitatZoneDto?         habitat      = ecology    != null ? await ecologyService.GetHabitatZoneAsync(ecology.EcologyId, ct)   : null;
        StockConservationDto?   conservation = firstStock != null ? await stockService.GetConservationAsync(firstStock.StockCode, ct) : null;
        StockEnvironmentDto?    environment  = firstStock != null ? await stockService.GetEnvironmentAsync(firstStock.StockCode, ct)  : null;

        // Presigned URLs — S3 không dùng DbContext, an toàn chạy song song
        var preferredPic = species.Pictures?.FirstOrDefault(p => p.PicPreferred    == true);
        var malePic      = species.Pictures?.FirstOrDefault(p => p.PicPreferredMale == true);
        var femalePic    = species.Pictures?.FirstOrDefault(p => p.PicPreferredFem  == true);

        async Task<string?> Presign(FishDex.EFCore.Entity.Media.SystemImage? pic) =>
            pic != null ? await storage.GetPresignedUrlAsync(pic.ObjectKey, ct) : null;

        var (preferredUrl, maleUrl, femaleUrl) = (
            await Presign(preferredPic),
            await Presign(malePic),
            await Presign(femalePic));

        return new SpeciesDetailDto
        {
            SpecCode            = species.SpecCode,
            SpeciesName         = species.SpeciesName,
            PreferredCommonName = species.CommonNames.PickPreferredName(language),
            GenusName           = species.Genus?.GenusName,
            FamilyName          = species.Family?.Name,
            WaterType           = species.WaterType.ToString(),
            Length              = species.Length,
            Weight              = species.Weight,
            Dangerous           = species.Dangerous,
            DemersPelag         = species.DemersPelag,
            LifeCycle           = species.LifeCycle,
            Remark              = species.Remark,
            PreferredImageUrl   = preferredUrl,
            MaleImageUrl        = maleUrl,
            FemaleImageUrl      = femaleUrl,
            Ecology = feeding != null || habitat != null ? new SpeciesDetailEcologyDto
            {
                FeedingType  = feeding?.FeedingType,
                DietTroph    = feeding?.DietTroph,
                HabitatZones = ExtractHabitatZones(habitat)
            } : null,
            Conservation = conservation != null ? new SpeciesDetailConservationDto
            {
                IucnCode        = conservation.IUCN_Code,
                IucnAssessment  = conservation.IUCN_Assessment,
                IucnDateAssessed = conservation.IUCN_DateAssessed,
                CitesCode       = conservation.CITES_Code
            } : null,
            Environment = environment != null ? new SpeciesDetailEnvironmentDto
            {
                TempMin = environment.TempMin,
                TempMax = environment.TempMax,
                PhMin   = environment.PHMin,
                PhMax   = environment.PHMax
            } : null
        };
    }

    private static IReadOnlyList<string> ExtractHabitatZones(HabitatZoneDto? hz)
    {
        if (hz == null) return [];
        var zones = new List<string>();
        if (hz.Neritic)   zones.Add("Neritic");
        if (hz.Estuaries) zones.Add("Estuaries");
        if (hz.Mangroves) zones.Add("Mangroves");
        if (hz.Stream)    zones.Add("Stream");
        if (hz.Lakes)     zones.Add("Lakes");
        return zones;
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

    private static string? NormalizeLanguage(string? lang) => lang?.ToLowerInvariant() switch
    {
        "vn" or "vi" or "vietnamese" => "Vietnamese",
        "en" or "eng" or "english"   => "English",
        null                          => null,
        var other                     => other
    };
}
