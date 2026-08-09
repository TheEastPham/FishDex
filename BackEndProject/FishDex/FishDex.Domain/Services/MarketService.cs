using FishDex.Domain.DTOs.Market;
using FishDex.Domain.Helpers;
using FishDex.Domain.Services.Interfaces;
using FishDex.EFCore.Cache;
using FishDex.EFCore.Entity.Market;
using FishDex.EFCore.Repository.Interface;
using FishLover.Shared.Common;
using FishLover.Shared.Services;
using Microsoft.Extensions.Logging;

namespace FishDex.Domain.Services;

/// <summary>
/// Lớp market. Xem <see cref="IMarketService"/> cho hợp đồng, và trang Epic trên Notion cho lý do.
/// </summary>
public class MarketService(
    ITradedSpeciesRepository repo,
    IFishBaseSpeciesIndexRepository indexRepo,
    ISpeciesCache speciesCache,
    IStorageService storage,
    ICurrentUserSession currentUser,
    ILogger<MarketService> logger) : IMarketService
{
    public IReadOnlyList<MarketCountryDto> GetCountries()
        => MarketCountries.Enabled
            .Select(c => new MarketCountryDto(c.Alpha2, c.NameEn, c.Languages))
            .ToList();

    public async Task<PagedResult<MarketSpeciesDto>?> GetSpeciesAsync(
        string alpha2, MarketSpeciesQuery query, CancellationToken ct = default)
    {
        var country = MarketCountries.ByAlpha2(alpha2);
        if (country is not { IsEnabled: true }) return null;

        var (min, max) = ToLengthRange(query.SizeBand);
        var hasLocalName = query.NameStatus switch
        {
            NameStatusFilter.Has     => true,
            NameStatusFilter.Missing => false,
            _                        => (bool?)null,
        };

        var page     = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var (rows, totalCount) = await repo.QueryAsync(new TradedSpeciesQuery
        {
            CountryCode  = country.Code,
            Languages    = country.Languages,
            MinLength    = min,
            MaxLength    = max,
            HasLocalName = hasLocalName,
            Page         = page,
            PageSize     = pageSize,
        }, ct);

        var items = await HydrateAsync(rows, ct);

        return new PagedResult<MarketSpeciesDto>
        {
            Items      = items,
            TotalCount = totalCount,
            Page       = page,
            PageSize   = pageSize,
        };
    }

    public async Task<MarketStatsDto?> GetStatsAsync(string alpha2, CancellationToken ct = default)
    {
        var country = MarketCountries.ByAlpha2(alpha2);
        if (country is not { IsEnabled: true }) return null;

        var (total, withName) = await repo.GetStatsAsync(country.Code, country.Languages, ct);
        return new MarketStatsDto(total, withName, total - withName);
    }

    public async Task<IReadOnlyList<SpeciesLookupDto>> LookupAsync(
        string query, int limit = 20, CancellationToken ct = default)
    {
        var hits = await indexRepo.SearchAsync(query, limit, ct);
        return hits
            .Select(i => new SpeciesLookupDto(
                i.SpecCode,
                i.SpeciesName,
                i.Genus,
                i.IsLoaded ? SpeciesLookupOutcome.InFishDex : SpeciesLookupOutcome.NeedsMigration))
            .ToList();
    }

    // ── Admin ─────────────────────────────────────────────────────

    public async Task<MarketMutationOutcome> AddAsync(
        string alpha2, AddTradedSpeciesRequest request, CancellationToken ct = default)
    {
        var country = MarketCountries.ByAlpha2(alpha2);
        if (country is null) return MarketMutationOutcome.CountryNotFound;

        if (RequiresLegalSource(request.LegalStatus, request.LegalSourceUrl))
            return MarketMutationOutcome.LegalSourceRequired;

        if (!await repo.SpecCodeExistsAsync(request.SpecCode, ct))
            return MarketMutationOutcome.SpeciesNotFound;

        if (await repo.GetAsync(country.Code, request.SpecCode, ct) is not null)
            return MarketMutationOutcome.AlreadyExists;

        var now = DateTime.UtcNow;
        await repo.AddAsync(new TradedSpecies
        {
            CountryCode     = country.Code,
            SpecCode        = request.SpecCode,
            TradeStatus     = request.TradeStatus,
            LegalStatus     = request.LegalStatus,
            LegalNote       = request.LegalNote?.Trim(),
            LegalSourceUrl  = request.LegalSourceUrl?.Trim(),
            Origin          = MarketOrigin.AdminAdded,
            Status          = MarketStatus.Approved,
            AddedBy         = currentUser.UserId,
            LastConfirmedAt = now,
        }, ct);
        await repo.SaveChangesAsync(ct);

        logger.LogInformation(
            "Market: admin {UserId} thêm SpecCode {SpecCode} vào {Country}",
            currentUser.UserId, request.SpecCode, country.Alpha2);

        return MarketMutationOutcome.Ok;
    }

    public async Task<MarketMutationOutcome> UpdateAsync(
        string alpha2, int specCode, UpdateTradedSpeciesRequest request, CancellationToken ct = default)
    {
        var country = MarketCountries.ByAlpha2(alpha2);
        if (country is null) return MarketMutationOutcome.CountryNotFound;

        if (RequiresLegalSource(request.LegalStatus, request.LegalSourceUrl))
            return MarketMutationOutcome.LegalSourceRequired;

        var row = await repo.GetAsync(country.Code, specCode, ct);
        if (row is null) return MarketMutationOutcome.NotFound;

        row.TradeStatus     = request.TradeStatus;
        row.LegalStatus     = request.LegalStatus;
        row.LegalNote       = request.LegalNote?.Trim();
        row.LegalSourceUrl  = request.LegalSourceUrl?.Trim();
        row.LastConfirmedAt = DateTime.UtcNow;

        await repo.SaveChangesAsync(ct);
        return MarketMutationOutcome.Ok;
    }

    public async Task<MarketMutationOutcome> RemoveAsync(
        string alpha2, int specCode, CancellationToken ct = default)
    {
        var country = MarketCountries.ByAlpha2(alpha2);
        if (country is null) return MarketMutationOutcome.CountryNotFound;

        var row = await repo.GetAsync(country.Code, specCode, ct);
        if (row is null) return MarketMutationOutcome.NotFound;

        repo.Remove(row);
        await repo.SaveChangesAsync(ct);

        logger.LogInformation(
            "Market: admin {UserId} gỡ SpecCode {SpecCode} khỏi {Country}",
            currentUser.UserId, specCode, country.Alpha2);

        return MarketMutationOutcome.Ok;
    }

    // ── Ingest ────────────────────────────────────────────────────

    public async Task<IngestResultDto?> IngestAsync(
        IngestTankSpeciesRequest request, CancellationToken ct = default)
    {
        var country = MarketCountries.ByAlpha2(request.CountryAlpha2);
        // Ingest chấp nhận cả nước chưa bật: dữ liệu cứ gom trước, bật trang sau.
        if (country is null) return null;

        var codes = request.SpecCodes.Where(c => c > 0).Distinct().ToList();
        if (codes.Count == 0) return new IngestResultDto(0, 0);

        var added = await repo.UpsertTankDerivedAsync(country.Code, codes, ct);
        await repo.SaveChangesAsync(ct);

        if (added > 0)
            logger.LogInformation(
                "Market ingest {Country}: nhận {Received} loài, thêm mới {Added}",
                country.Alpha2, codes.Count, added);

        return new IngestResultDto(codes.Count, added);
    }

    // ── Helper ────────────────────────────────────────────────────

    /// <summary>
    /// Đổi hàng repository thành DTO. Tên và ảnh lấy từ <c>SpeciesSnapshot</c> — gọi
    /// <c>GetOrPopulateManyAsync</c> cho cả trang một lượt để loài chưa ai xem cũng có dữ liệu,
    /// thay vì để trống. Presign chạy sau vì <c>ObjectKey</c> là computed property.
    /// </summary>
    private async Task<List<MarketSpeciesDto>> HydrateAsync(
        IReadOnlyList<TradedSpeciesRow> rows, CancellationToken ct)
    {
        if (rows.Count == 0) return [];

        var snapshots = await speciesCache.GetOrPopulateManyAsync(rows.Select(r => r.SpecCode), ct);
        var byCode = snapshots.ToDictionary(s => s.SpecCode);

        var result = new List<MarketSpeciesDto>(rows.Count);
        foreach (var row in rows)
        {
            byCode.TryGetValue(row.SpecCode, out var snap);

            // S3 presign là ký HMAC local, không gọi mạng — tuần tự ở đây là chấp nhận được.
            var imageUrl = snap?.ThumbnailObjectKey is { } key
                ? await storage.GetPresignedUrlAsync(key, ct)
                : null;

            result.Add(new MarketSpeciesDto(
                SpecCode:       row.SpecCode,
                ScientificName: snap?.SpeciesName ?? $"SpecCode {row.SpecCode}",
                LocalName:      row.LocalName,
                ImageUrl:       imageUrl,
                LengthCm:       row.Length ?? snap?.Length,
                TradeStatus:    row.TradeStatus,
                LegalStatus:    row.LegalStatus,
                LegalNote:      row.LegalNote,
                Vulnerability:  row.Vulnerability,
                Dangerous:      row.Dangerous));
        }

        return result;
    }

    /// <summary>Restricted hoặc Banned mà không có nguồn là đoán — tôn chỉ dự án không cho phép.</summary>
    private static bool RequiresLegalSource(LegalStatus status, string? sourceUrl)
        => status != LegalStatus.Legal && string.IsNullOrWhiteSpace(sourceUrl);

    private static (decimal? Min, decimal? Max) ToLengthRange(SizeBand? band) => band switch
    {
        SizeBand.Under5     => (null, 5m),
        SizeBand.From5To10  => (5m,   10m),
        SizeBand.From10To20 => (10m,  20m),
        SizeBand.Over20     => (20m,  null),
        _                   => (null, null),
    };
}
