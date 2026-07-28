using System.IO;
using FishDex.Domain.DTOs.Ecologies;
using FishDex.Domain.DTOs.Species;
using FishDex.Domain.DTOs.Stocks;
using FishDex.Domain.Mappings;
using FishDex.Domain.Services.Interfaces;
using FishDex.Domain.Settings;
using FishDex.EFCore.Cache;
using FishDex.EFCore.Entity.Cache;
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
    ISpeciesCache speciesCache,
    ICommunitySpeciesRepository communityRepo,
    IMemoryCache cache,
    IOptions<FishDexSettings> settings) : ISpeciesService
{
    private readonly int _languageTopCount = settings.Value.LanguageTopCount;

    // Loài community (lai tạo, do user submit) dùng SpecCode ≥ ngưỡng này; dưới ngưỡng là FishBase.
    private const int CommunityMinSpecCode = 500_000;

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
        var names = await commonNameRepo.FindAsync(c => c.SpecCode == specCode && c.IsVerified);
        return names.OrderByDescending(c => c.IsPreferred).ThenBy(c => c.Rank).Select(c => c.ToDto()).ToList();
    }

    public async Task<IReadOnlyList<CommonNameDto>> SearchCommonNamesAsync(string term, string? language = null, CancellationToken ct = default)
    {
        var names = await commonNameRepo.FindAsync(c =>
            c.ComName.Contains(term) && c.IsVerified &&
            (language == null || c.Language == language));
        return names.OrderByDescending(c => c.IsPreferred).ThenBy(c => c.Rank).Select(c => c.ToDto()).ToList();
    }

    public async Task<PagedResult<SpeciesSearchResultDto>> SearchSpeciesAsync(
        GetSpeciesSearchRequest request, CancellationToken ct = default)
    {
        var language = NormalizeLanguage(request.Language);
        var skip = (request.Page - 1) * request.PageSize;

        // Loài community (lai tạo, verified) chỉ xuất hiện khi KHÔNG lọc theo họ/chi FishBase —
        // chúng lưu FamilyName/GenusName dạng free text, không có FK taxonomy để filter.
        var includeCommunity = request.FamId is null && request.GenusCode is null;

        var communityAll = includeCommunity
            ? await MapCommunitySearchAsync(await communityRepo.SearchVerifiedAsync(request.Query, ct), ct)
            : [];
        var commCount = communityAll.Count;

        // Thứ tự: community trước, FishBase sau — phân trang xuyên suốt 2 nguồn.
        var communityPage = communityAll.Skip(skip).Take(request.PageSize).ToList();
        var remaining = request.PageSize - communityPage.Count;

        var fbSkip = Math.Max(0, skip - commCount);
        var (fbItems, fbTotal) = await speciesRepo.SearchSliceAsync(
            request.Query, request.FamId, request.GenusCode, language, fbSkip, remaining, ct);

        var fbMapped = await Task.WhenAll(fbItems.Select(async s =>
        {
            var pic      = s.Pictures.FirstOrDefault(p => p.PicPreferred == true);
            var imageUrl = pic != null
                ? await storage.GetPresignedUrlAsync(pic.ObjectKey, ct)
                : null;
            return s.ToSearchResultDto(language, imageUrl);
        }));

        return new PagedResult<SpeciesSearchResultDto>
        {
            Items      = [.. communityPage, .. fbMapped],
            TotalCount = commCount + fbTotal,
            Page       = request.Page,
            PageSize   = request.PageSize
        };
    }

    private async Task<List<SpeciesSearchResultDto>> MapCommunitySearchAsync(
        IReadOnlyList<SpeciesSnapshot> items, CancellationToken ct)
    {
        var result = new List<SpeciesSearchResultDto>(items.Count);
        foreach (var s in items)
        {
            var imageUrl = s.ThumbnailObjectKey != null
                ? await storage.GetPresignedUrlAsync(s.ThumbnailObjectKey, ct)
                : null;
            result.Add(new SpeciesSearchResultDto
            {
                SpecCode            = s.SpecCode,
                SpeciesName         = s.SpeciesName,
                PreferredCommonName = s.CommonName,
                GenusName           = s.GenusName,
                FamilyName          = s.FamilyName,
                ImageUrl            = imageUrl,
            });
        }
        return result;
    }

    private async Task<SpeciesDetailDto?> BuildCommunityDetailAsync(int specCode, CancellationToken ct)
    {
        var s = await communityRepo.GetVerifiedByCodeAsync(specCode, ct);
        if (s is null) return null;

        var imageUrl = s.ThumbnailObjectKey != null
            ? await storage.GetPresignedUrlAsync(s.ThumbnailObjectKey, ct)
            : null;

        var hasEcology = s.FeedingType != null || s.Schooling != null || s.Shoaling != null || s.Solitary != null;
        var hasEnv = s.TempMin != null || s.TempMax != null || s.PhMin != null
                     || s.PhMax != null || s.DhMin != null || s.DhMax != null;

        return new SpeciesDetailDto
        {
            SpecCode            = s.SpecCode,
            SpeciesName         = s.SpeciesName,
            PreferredCommonName = s.CommonName,
            GenusName           = s.GenusName,
            FamilyName          = s.FamilyName,
            WaterType           = s.WaterType.ToString(),
            Length              = s.Length,
            DemersPelag         = s.DemersPelag,
            LongevityCaptive    = s.LongevityCaptive,
            PreferredImageUrl   = imageUrl,
            Ecology = hasEcology ? new SpeciesDetailEcologyDto
            {
                FeedingType = s.FeedingType,
                Schooling   = s.Schooling,
                Shoaling    = s.Shoaling,
                Solitary    = s.Solitary,
            } : null,
            Environment = hasEnv ? new SpeciesDetailEnvironmentDto
            {
                TempMin = s.TempMin,
                TempMax = s.TempMax,
                PhMin   = s.PhMin,
                PhMax   = s.PhMax,
                DHMin   = s.DhMin,
                DHMax   = s.DhMax,
            } : null,
        };
    }

    public async Task<SpeciesDetailDto?> GetDetailAsync(int specCode, string? language = null, CancellationToken ct = default)
    {
        language = NormalizeLanguage(language);

        // Community codes (≥ 500000) không nằm trong FishBase tables → build detail từ SpeciesSnapshot.
        if (specCode >= CommunityMinSpecCode)
            return await BuildCommunityDetailAsync(specCode, ct);

        // Trigger cache population (cache-aside) — does nothing if already cached
        // Must await: shares the same scoped DbContext, concurrent use is not safe
        await speciesCache.GetOrPopulateAsync(specCode, ct);

        var species = await speciesRepo.GetWithDetailsAsync(specCode, ct);
        if (species == null) return null;

        // Sequential — EF Core DbContext không thread-safe, không dùng Task.WhenAll với cùng scope
        var ecology    = await ecologyService.GetBySpecCodeAsync(specCode, ct);
        var stocks     = await stockService.GetBySpecCodeAsync(specCode, ct);
        var firstStock = stocks.FirstOrDefault();

        FeedingAndDietDto?      feeding        = ecology != null ? await ecologyService.GetFeedingAsync(ecology.EcologyId, ct)         : null;
        HabitatZoneDto?         habitat        = ecology != null ? await ecologyService.GetHabitatZoneAsync(ecology.EcologyId, ct)     : null;
        AssociationsDto?        associations   = ecology != null ? await ecologyService.GetAssociationsAsync(ecology.EcologyId, ct)    : null;
        SubstrateDto?           substrate      = ecology != null ? await ecologyService.GetSubstrateAsync(ecology.EcologyId, ct)       : null;
        SpecialHabitatDto?      specialHabitat = ecology != null ? await ecologyService.GetSpecialHabitatAsync(ecology.EcologyId, ct)  : null;
        StockConservationDto?   conservation = firstStock != null ? await stockService.GetConservationAsync(firstStock.StockCode, ct)  : null;
        StockEnvironmentDto?    environment  = firstStock != null ? await stockService.GetEnvironmentAsync(firstStock.StockCode, ct)   : null;

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
            // Taxonomy
            ClassName           = species.Family?.Class,
            OrderName           = species.Family?.Order,
            // Lifespan
            LongevityWild       = species.LongevityWild,
            LongevityCaptive    = species.LongevityCaptive,
            PreferredImageUrl   = preferredUrl,
            MaleImageUrl        = maleUrl,
            FemaleImageUrl      = femaleUrl,
            Ecology = feeding != null || habitat != null || associations != null ? new SpeciesDetailEcologyDto
            {
                FeedingType  = feeding?.FeedingType,
                DietTroph    = feeding?.DietTroph,
                HabitatZones = ExtractHabitatZones(habitat),
                // Đọc từ Associations (nguồn đúng trong FishBase) — không phải Ecology entity
                Schooling    = associations?.Schooling,
                Shoaling     = associations?.Shoaling,
                Solitary     = associations?.Solitary
            } : null,
            Conservation = conservation != null ? new SpeciesDetailConservationDto
            {
                IucnCode         = conservation.IUCN_Code,
                IucnAssessment   = conservation.IUCN_Assessment,
                IucnDateAssessed = conservation.IUCN_DateAssessed,
                CitesCode        = conservation.CITES_Code
            } : null,
            Habitat = substrate != null || specialHabitat != null ? new SpeciesDetailHabitatDto
            {
                PreferredSubstrates = substrate?.PreferredSubstrates ?? [],
                BurrowingCapable    = substrate?.BurrowingCapable ?? false,
                RequiresCaves       = specialHabitat?.RequiresCaves ?? false,
                RequiresDriftwood   = specialHabitat?.RequiresDriftwood ?? false,
                RequiresVegetation  = specialHabitat?.RequiresVegetation ?? false,
                RequiresCoralReefs  = specialHabitat?.RequiresCoralReefs ?? false,
                SpecialHabitats     = specialHabitat?.SpecialHabitats ?? []
            } : null,
            Environment = environment != null ? new SpeciesDetailEnvironmentDto
            {
                TempMin          = environment.TempMin,
                TempMax          = environment.TempMax,
                TempPreferred    = environment.TempPreferred,
                PhMin            = environment.PHMin,
                PhMax            = environment.PHMax,
                DHMin            = environment.DHMin,
                DHMax            = environment.DHMax,
                Resilience       = environment.Resilience,
                ResilienceRemark = environment.ResilienceRemark
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

    public async Task<IReadOnlyList<SpeciesSearchResultDto>> GetRelatedAsync(
        int specCode, int limit = 6, string? language = null, CancellationToken ct = default)
    {
        language = NormalizeLanguage(language);
        limit    = Math.Clamp(limit, 3, 6);

        var species = await speciesRepo.GetWithDetailsAsync(specCode, ct);
        if (species == null) return [];

        var related = await speciesRepo.GetRelatedAsync(
            specCode, species.GenusCode, species.FamId, limit, ct);

        return await Task.WhenAll(related.Select(async s =>
        {
            var pic      = s.Pictures?.FirstOrDefault(p => p.PicPreferred == true);
            var imageUrl = pic != null
                ? await storage.GetPresignedUrlAsync(pic.ObjectKey, ct)
                : null;
            return s.ToSearchResultDto(language, imageUrl);
        }));
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

    public async Task<IReadOnlyList<SpeciesSummaryDto>> GetSummariesAsync(
        IEnumerable<int> specCodes, string? language = null, CancellationToken ct = default)
    {
        language = NormalizeLanguage(language);

        // Use snapshot cache — avoids loading 16 FishBase tables per species
        var snapshots = await speciesCache.GetOrPopulateManyAsync(specCodes, ct);

        return await Task.WhenAll(snapshots.Select(async snap =>
        {
            var imageUrl = snap.ThumbnailObjectKey is not null
                ? await storage.GetPresignedUrlAsync(snap.ThumbnailObjectKey, ct)
                : null;
            return new SpeciesSummaryDto
            {
                SpecCode    = snap.SpecCode,
                SpeciesName = snap.SpeciesName,
                CommonName  = snap.CommonName,
                ImageUrl    = imageUrl
            };
        }));
    }

    private static string? NormalizeLanguage(string? lang) => lang?.ToLowerInvariant() switch
    {
        "vn" or "vi" or "vietnamese" => "Vietnamese",
        "en" or "eng" or "english"   => "English",
        null                          => null,
        var other                     => other
    };
}
