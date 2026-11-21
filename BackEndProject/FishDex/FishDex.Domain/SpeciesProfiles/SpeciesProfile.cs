using FishDex.Domain.DTOs.Species;
using FishDex.Domain.Mappings;
using FishDex.Domain.Services.Interfaces;
using FishDex.EFCore.DbContexts;
using FishDex.EFCore.Entity.Cache;
using FishDex.EFCore.Entity.Media;
using FishDex.EFCore.Entity.Species;
using FishDex.EFCore.Repository.Interface;
using FishLover.Storage;
using FishLover.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace FishDex.Domain.SpeciesProfiles;

/// <summary>
/// Đọc thẳng bảng FishBase gốc bằng truy vấn gom theo lô (dữ liệu FishBase chỉ đổi khi chạy ETL),
/// và đọc loài cộng đồng từ <c>SpeciesSnapshots</c>. Không có cache tầng DB: không ghi trong GET,
/// không có chuyện snapshot cũ. Rule chọn ảnh/tên/ecology ở <see cref="SpeciesRules"/>.
/// </summary>
public sealed class SpeciesProfile(FishDexDbContext db, ICommunitySpeciesRepository communityRepo, IStorageService storage) : ISpeciesProfile
{
    // ── Detail ───────────────────────────────────────────────────────────────

    public async Task<SpeciesDetailDto?> GetDetailAsync(int specCode, string? language = null, CancellationToken ct = default)
    {
        language = SpeciesRules.NormalizeLanguage(language);

        return SpeciesCodes.IsCommunity(specCode)
            ? await GetCommunityDetailAsync(specCode, ct)
            : await GetFishBaseDetailAsync(specCode, language, ct);
    }

    private async Task<SpeciesDetailDto?> GetFishBaseDetailAsync(int specCode, string? language, CancellationToken ct)
    {
        // Tuần tự — DbContext không thread-safe.
        var species = await db.Species
            .AsNoTracking()
            .Include(s => s.Genus)
            .Include(s => s.Family)
            .Include(s => s.CommonNames.Where(c => c.IsVerified))
            .FirstOrDefaultAsync(s => s.SpecCode == specCode, ct);
        if (species == null) return null;

        var images = await db.SystemImages
            .AsNoTracking()
            .Where(i => i.SpecCode == specCode)
            .ToListAsync(ct);
        var picked = SpeciesRules.PickImages(images, species.PicPreferredNameM, species.PicPreferredNameF);

        var ecology = SpeciesRules.PickEcology(await db.Ecologies
            .AsNoTracking()
            .Where(e => e.SpecCode == specCode)
            .ToListAsync(ct));

        var ecologyId = ecology?.EcologyId;
        var feeding = ecologyId is { } f1
            ? await db.FeedingAndDiets.AsNoTracking().FirstOrDefaultAsync(x => x.EcologyId == f1, ct) : null;
        var habitat = ecologyId is { } f2
            ? await db.HabitatZones.AsNoTracking().FirstOrDefaultAsync(x => x.EcologyId == f2, ct) : null;
        var associations = ecologyId is { } f3
            ? await db.Associations.AsNoTracking().FirstOrDefaultAsync(x => x.EcologyId == f3, ct) : null;
        var substrate = ecologyId is { } f4
            ? (await db.Substrates.AsNoTracking().FirstOrDefaultAsync(x => x.EcologyId == f4, ct))?.ToDto() : null;
        var specialHabitat = ecologyId is { } f5
            ? (await db.SpecialHabitats.AsNoTracking().FirstOrDefaultAsync(x => x.EcologyId == f5, ct))?.ToDto() : null;

        // Stock đầu tiên (StockCode nhỏ nhất) — conservation + environment đi kèm trong 1 query.
        var stock = await db.Stocks
            .AsNoTracking()
            .Include(s => s.Conservation)
            .Include(s => s.Environment)
            .Where(s => s.SpecCode == specCode)
            .OrderBy(s => s.StockCode)
            .FirstOrDefaultAsync(ct);
        var conservation = stock?.Conservation;
        var environment  = stock?.Environment;

        return new SpeciesDetailDto
        {
            SpecCode            = species.SpecCode,
            SpeciesName         = species.SpeciesName,
            PreferredCommonName = SpeciesRules.PickCommonName(species.CommonNames, language),
            GenusName           = species.Genus?.GenusName,
            FamilyName          = species.Family?.Name,
            WaterType           = species.WaterType.ToString(),
            Length              = species.Length,
            Weight              = species.Weight,
            Dangerous           = species.Dangerous,
            DemersPelag         = species.DemersPelag,
            LifeCycle           = species.LifeCycle,
            Remark              = species.Remark,
            ClassName           = species.Family?.Class,
            OrderName           = species.Family?.Order,
            LongevityWild       = species.LongevityWild,
            LongevityCaptive    = species.LongevityCaptive,
            PreferredImageUrl   = await PresignAsync(picked.Thumbnail, ct),
            MaleImageUrl        = await PresignAsync(picked.Male, ct),
            FemaleImageUrl      = await PresignAsync(picked.Female, ct),
            PreferredImageKey   = picked.Thumbnail?.ObjectKey,
            MaleImageKey        = picked.Male?.ObjectKey,
            FemaleImageKey      = picked.Female?.ObjectKey,
            Ecology = feeding != null || habitat != null || associations != null ? new SpeciesDetailEcologyDto
            {
                FeedingType  = feeding?.FeedingType,
                DietTroph    = SpeciesRules.PickTrophicLevel(feeding),
                HabitatZones = SpeciesRules.HabitatZones(habitat),
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
                Resilience       = environment.Resilience?.ToString(),
                ResilienceRemark = environment.ResilienceRemark
            } : null
        };
    }

    private async Task<SpeciesDetailDto?> GetCommunityDetailAsync(int specCode, CancellationToken ct)
    {
        var s = await communityRepo.GetVerifiedByCodeAsync(specCode, ct);
        if (s is null) return null;

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
            PreferredImageUrl   = await PresignAsync(s.ThumbnailObjectKey, ct),
            PreferredImageKey   = s.ThumbnailObjectKey,
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

    // ── Summaries ────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<SpeciesSummaryDto>> GetSummariesAsync(
        IEnumerable<int> specCodes, string? language = null, CancellationToken ct = default)
    {
        language = SpeciesRules.NormalizeLanguage(language);

        var codes = specCodes.Distinct().ToList();
        if (codes.Count == 0) return [];

        var communityCodes = codes.Where(SpeciesCodes.IsCommunity).ToList();
        var fishBaseCodes  = codes.Where(c => !SpeciesCodes.IsCommunity(c)).ToList();

        // Loài cộng đồng: mọi trạng thái duyệt — AquaHome có thể đã lưu mã của loài đang chờ duyệt.
        var community = communityCodes.Count == 0
            ? []
            : await db.SpeciesSnapshots
                .AsNoTracking()
                .Where(s => s.DataSource == SnapshotDataSource.Community && communityCodes.Contains(s.SpecCode))
                .ToListAsync(ct);

        var fishBase = fishBaseCodes.Count == 0
            ? []
            : await db.Species
                .AsNoTracking()
                .Where(s => fishBaseCodes.Contains(s.SpecCode))
                .Select(s => new { s.SpecCode, s.SpeciesName, s.Length })
                .ToListAsync(ct);

        var names  = await LoadVerifiedNamesAsync(fishBaseCodes, ct);
        var images = await LoadImagesAsync(fishBase.Select(s => s.SpecCode).ToList(), ct);

        var byCode = new Dictionary<int, (string Name, string? CommonName, string? ImageKey, decimal? Length)>();
        foreach (var s in community)
            byCode[s.SpecCode] = (s.SpeciesName, s.CommonName, s.ThumbnailObjectKey, s.Length);
        foreach (var s in fishBase)
            byCode[s.SpecCode] = (s.SpeciesName, SpeciesRules.PickCommonName(names[s.SpecCode], language),
                                  SpeciesRules.PickThumbnail(images[s.SpecCode])?.ObjectKey, s.Length);

        var result = new List<SpeciesSummaryDto>(byCode.Count);
        foreach (var code in codes)
        {
            if (!byCode.TryGetValue(code, out var card)) continue;
            result.Add(new SpeciesSummaryDto
            {
                SpecCode    = code,
                SpeciesName = card.Name,
                CommonName  = card.CommonName,
                ImageUrl    = await PresignAsync(card.ImageKey, ct),
                ImageKey    = card.ImageKey,
                Length      = card.Length,
            });
        }
        return result;
    }

    // ── Search ───────────────────────────────────────────────────────────────

    public async Task<PagedResult<SpeciesSearchResultDto>> SearchAsync(GetSpeciesSearchRequest request, CancellationToken ct = default)
    {
        var language = SpeciesRules.NormalizeLanguage(request.Language);
        var skip = (request.Page - 1) * request.PageSize;

        // Loài community (lai tạo, verified) chỉ xuất hiện khi KHÔNG lọc theo họ/chi FishBase —
        // chúng lưu FamilyName/GenusName dạng free text, không có FK taxonomy để filter.
        var includeCommunity = request.FamId is null && request.GenusCode is null;
        var communityAll = includeCommunity
            ? await communityRepo.SearchVerifiedAsync(request.Query, ct)
            : [];

        // Thứ tự: community trước, FishBase sau — phân trang xuyên suốt 2 nguồn.
        var communityPage = communityAll.Skip(skip).Take(request.PageSize).ToList();
        var remaining = request.PageSize - communityPage.Count;
        var fbSkip = Math.Max(0, skip - communityAll.Count);

        var fbQuery = BuildFishBaseSearchQuery(request.Query, request.FamId, request.GenusCode, language);
        var fbTotal = await fbQuery.CountAsync(ct);
        var fbItems = remaining <= 0
            ? []
            : await fbQuery
                .OrderBy(s => s.SpeciesName)
                .Skip(fbSkip)
                .Take(remaining)
                .ToListAsync(ct);

        var items = new List<SpeciesSearchResultDto>(communityPage.Count + fbItems.Count);
        items.AddRange(await ToCommunityCardsAsync(communityPage, ct));
        items.AddRange(await ToFishBaseCardsAsync(fbItems, language, ct));

        return new PagedResult<SpeciesSearchResultDto>
        {
            Items      = items,
            TotalCount = communityAll.Count + fbTotal,
            Page       = request.Page,
            PageSize   = request.PageSize
        };
    }

    private IQueryable<Species> BuildFishBaseSearchQuery(string? query, Guid? famId, int? genusCode, string? language)
    {
        var hasQuery = !string.IsNullOrWhiteSpace(query);
        var q = db.Species
            .AsNoTracking()
            .Include(s => s.Genus)
            .Include(s => s.Family)
            .Include(s => s.CommonNames.Where(c => c.IsVerified))
            .Where(s =>
                !hasQuery ||
                EF.Functions.ILike(s.SpeciesName, $"%{query}%") ||
                s.CommonNames.Any(c =>
                    c.IsVerified &&
                    EF.Functions.ILike(c.ComName, $"%{query}%") &&
                    (language == null || c.Language == language)));

        if (famId.HasValue)     q = q.Where(s => s.FamId == famId.Value);
        if (genusCode.HasValue) q = q.Where(s => s.GenusCode == genusCode.Value);

        return q;
    }

    // ── Related ──────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<SpeciesSearchResultDto>> GetRelatedAsync(
        int specCode, int limit = 6, string? language = null, CancellationToken ct = default)
    {
        language = SpeciesRules.NormalizeLanguage(language);
        limit    = Math.Clamp(limit, 3, 6);

        var anchor = await db.Species
            .AsNoTracking()
            .Where(s => s.SpecCode == specCode)
            .Select(s => new { s.GenusCode, s.FamId })
            .FirstOrDefaultAsync(ct);
        if (anchor == null) return [];

        var baseQuery = db.Species
            .AsNoTracking()
            .Include(s => s.Genus)
            .Include(s => s.Family)
            .Include(s => s.CommonNames.Where(c => c.IsVerified))
            .Where(s => s.SpecCode != specCode);

        // Ưu tiên cùng Genus
        var sameGenus = anchor.GenusCode is { } genusCode
            ? await baseQuery
                .Where(s => s.GenusCode == genusCode)
                .OrderBy(s => s.SpeciesName)
                .Take(limit)
                .ToListAsync(ct)
            : [];

        var related = sameGenus;
        if (sameGenus.Count < limit)
        {
            // Backfill bằng cùng Family
            var alreadyFound = sameGenus.Select(s => s.SpecCode).ToList();
            var fromFamily = await baseQuery
                .Where(s => s.FamId == anchor.FamId && !alreadyFound.Contains(s.SpecCode))
                .OrderBy(s => s.SpeciesName)
                .Take(limit - sameGenus.Count)
                .ToListAsync(ct);
            related = [.. sameGenus, .. fromFamily];
        }

        return await ToFishBaseCardsAsync(related, language, ct);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<List<SpeciesSearchResultDto>> ToFishBaseCardsAsync(IReadOnlyList<Species> species, string? language, CancellationToken ct)
    {
        var images = await LoadImagesAsync(species.Select(s => s.SpecCode).ToList(), ct);

        var result = new List<SpeciesSearchResultDto>(species.Count);
        foreach (var s in species)
        {
            var thumb = SpeciesRules.PickThumbnail(images[s.SpecCode]);
            result.Add(new SpeciesSearchResultDto
            {
                SpecCode            = s.SpecCode,
                SpeciesName         = s.SpeciesName,
                PreferredCommonName = SpeciesRules.PickCommonName(s.CommonNames, language),
                GenusName           = s.Genus?.GenusName,
                FamilyName          = s.Family?.Name,
                ImageUrl            = await PresignAsync(thumb, ct),
                ImageKey            = thumb?.ObjectKey,
            });
        }
        return result;
    }

    private async Task<List<SpeciesSearchResultDto>> ToCommunityCardsAsync(IReadOnlyList<SpeciesSnapshot> items, CancellationToken ct)
    {
        var result = new List<SpeciesSearchResultDto>(items.Count);
        foreach (var s in items)
        {
            result.Add(new SpeciesSearchResultDto
            {
                SpecCode            = s.SpecCode,
                SpeciesName         = s.SpeciesName,
                PreferredCommonName = s.CommonName,
                GenusName           = s.GenusName,
                FamilyName          = s.FamilyName,
                ImageUrl            = await PresignAsync(s.ThumbnailObjectKey, ct),
                ImageKey            = s.ThumbnailObjectKey,
            });
        }
        return result;
    }

    private async Task<ILookup<int, CommonName>> LoadVerifiedNamesAsync(IReadOnlyList<int> codes, CancellationToken ct)
    {
        if (codes.Count == 0) return Array.Empty<CommonName>().ToLookup(c => c.SpecCode);
        var names = await db.CommonNames
            .AsNoTracking()
            .Where(c => c.IsVerified && codes.Contains(c.SpecCode))
            .ToListAsync(ct);
        return names.ToLookup(c => c.SpecCode);
    }

    private async Task<ILookup<int, SystemImage>> LoadImagesAsync(IReadOnlyList<int> codes, CancellationToken ct)
    {
        if (codes.Count == 0) return Array.Empty<SystemImage>().ToLookup(i => i.SpecCode);
        var images = await db.SystemImages
            .AsNoTracking()
            .Where(i => codes.Contains(i.SpecCode))
            .ToListAsync(ct);
        return images.ToLookup(i => i.SpecCode);
    }

    private Task<string?> PresignAsync(SystemImage? image, CancellationToken ct)
    {
        return PresignAsync(image?.ObjectKey, ct);
    }

    private async Task<string?> PresignAsync(string? objectKey, CancellationToken ct)
    {
        if (objectKey is null) return null;
        return await storage.GetPresignedUrlAsync(objectKey, ct);
    }
}
