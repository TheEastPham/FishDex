using FishDex.EFCore.DbContexts;
using FishDex.EFCore.Entity.Cache;
using FishDex.EFCore.Entity.Market;
using FishDex.EFCore.Entity.Species;
using FishDex.EFCore.Repository.Interface;
using Microsoft.EntityFrameworkCore;

namespace FishDex.EFCore.Repository;

public class TradedSpeciesRepository(FishDexDbContext db) : ITradedSpeciesRepository
{
    private const int CommunityMinSpecCode = 500_000;

    public async Task<(IReadOnlyList<TradedSpeciesRow> Items, int TotalCount)> QueryAsync(
        TradedSpeciesQuery query, CancellationToken ct = default)
    {
        var filtered = BuildFilteredQuery(query);

        var totalCount = await filtered.CountAsync(ct);

        var verifiedNames = BuildVerifiedNameQuery(query.Languages);

        // Sắp xếp và phân trang TRƯỚC, chiếu sang record SAU CÙNG.
        // Nếu OrderBy đứng sau Select thì EF phải dựng record bên trong ORDER BY và
        // không dịch được — đã dính đúng lỗi đó một lần.
        var items = await filtered
            .OrderBy(t => t.SpecCode)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(t => new TradedSpeciesRow(
                t.SpecCode,
                db.Species
                    .Where(s => s.SpecCode == t.SpecCode)
                    .Select(s => s.Length)
                    .FirstOrDefault(),
                verifiedNames
                    .Where(c => c.SpecCode == t.SpecCode)
                    .OrderByDescending(c => c.IsPreferred)
                    .Select(c => c.ComName)
                    .FirstOrDefault(),
                t.TradeStatus,
                t.LegalStatus,
                t.LegalNote,
                db.Species
                    .Where(s => s.SpecCode == t.SpecCode)
                    .Select(s => s.Vulnerability)
                    .FirstOrDefault(),
                db.Species
                    .Where(s => s.SpecCode == t.SpecCode)
                    .Select(s => s.Dangerous)
                    .FirstOrDefault()))
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<(int TotalCount, int WithLocalName)> GetStatsAsync(string countryCode, string[] languages, CancellationToken ct = default)
    {
        var approved = db.TradedSpecies.AsNoTracking();
        approved = approved.Where(t => t.CountryCode == countryCode);
        approved = approved.Where(t => t.Status == MarketStatus.Approved);

        var totalCount = await approved.CountAsync(ct);

        var verifiedNames = BuildVerifiedNameQuery(languages);
        var named = approved.Where(t => verifiedNames.Any(c => c.SpecCode == t.SpecCode));
        var withLocalName = await named.CountAsync(ct);

        return (totalCount, withLocalName);
    }

    public async Task<TradedSpecies?> GetAsync(string countryCode, int specCode, CancellationToken ct = default)
    {
        return await db.TradedSpecies
            .Where(t => t.CountryCode == countryCode)
            .Where(t => t.SpecCode == specCode)
            .FirstOrDefaultAsync(ct);
    }

    public async Task AddAsync(TradedSpecies entity, CancellationToken ct = default)
    {
        await db.TradedSpecies.AddAsync(entity, ct);
    }

    public void Remove(TradedSpecies entity)
    {
        db.TradedSpecies.Remove(entity);
    }

    public async Task<int> UpsertTankDerivedAsync(
        string countryCode, IReadOnlyCollection<int> specCodes, CancellationToken ct = default)
    {
        if (specCodes.Count == 0)
        {
            return 0;
        }

        var now = DateTime.UtcNow;

        var affected = db.TradedSpecies.Where(t => t.CountryCode == countryCode);
        affected = affected.Where(t => specCodes.Contains(t.SpecCode));

        var existing = await affected.ToListAsync(ct);

        // Loài đã có: chỉ đẩy mốc xác nhận, không đụng gì khác — admin có thể đã curate
        // TradeStatus hoặc LegalStatus cho dòng đó rồi.
        foreach (var row in existing)
        {
            row.LastConfirmedAt = now;
        }

        var existingCodes = existing
            .Select(t => t.SpecCode)
            .ToHashSet();

        var newRows = specCodes
            .Where(code => !existingCodes.Contains(code))
            .Select(code => new TradedSpecies
            {
                CountryCode = countryCode,
                SpecCode = code,
                Origin = MarketOrigin.TankDerived,
                Status = MarketStatus.Approved,
                LegalStatus = LegalStatus.Legal,
                AddedBy = null, // KHÔNG lưu tham chiếu user — quy tắc một chiều
                TradeStatus = null, // mức phổ biến chỉ do người curate
                LastConfirmedAt = now,
            })
            .ToList();

        if (newRows.Count > 0)
        {
            await db.TradedSpecies.AddRangeAsync(newRows, ct);
        }

        return newRows.Count;
    }

    public async Task<bool> SpecCodeExistsAsync(int specCode, CancellationToken ct = default)
    {
        if (specCode < CommunityMinSpecCode)
            return await db.Species.AnyAsync(s => s.SpecCode == specCode, ct);

        return await db.SpeciesSnapshots
            .Where(s => s.SpecCode == specCode)
            .Where(s => s.DataSource == SnapshotDataSource.Community)
            .Where(s => s.IsVerified)
            .AnyAsync(ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// CHỈ lọc, giữ nguyên kiểu entity để <c>OrderBy</c> và phân trang còn dịch được sang SQL.
    /// Việc chiếu sang <see cref="TradedSpeciesRow"/> phải làm SAU CÙNG ở <c>QueryAsync</c>.
    ///
    /// <para>Lọc kích thước đi qua một <c>Any</c> tương quan sang Species. Loài lai không có row
    /// bên đó nên tự rơi ra ngoài khi chọn khoảng kích thước — đúng ý: không có số đo thì không
    /// khẳng định nó thuộc khoảng nào.</para>
    ///
    /// <para>Điều kiện bên trong <c>Any</c> nằm ở Where con nên gộp bằng <c>&amp;&amp;</c>.</para>
    /// </summary>
    private IQueryable<TradedSpecies> BuildFilteredQuery(TradedSpeciesQuery query)
    {
        var filtered = db.TradedSpecies.AsNoTracking()
            .Where(t => t.CountryCode == query.CountryCode)
            .Where(t => t.Status == MarketStatus.Approved);

        if (query.MinLength.HasValue)
        {
            filtered = filtered.Where(t => db.Species
                .Any(s => s.SpecCode == t.SpecCode && s.Length >= query.MinLength.Value));
        }

        if (query.MaxLength.HasValue)
        {
            filtered = filtered.Where(t => db.Species
                .Any(s => s.SpecCode == t.SpecCode && s.Length < query.MaxLength.Value));
        }

        if (query.HasLocalName == true)
        {
            var named = BuildVerifiedNameQuery(query.Languages);
            filtered = filtered.Where(t => named.Any(c => c.SpecCode == t.SpecCode));
        }

        if (query.HasLocalName == false)
        {
            var named = BuildVerifiedNameQuery(query.Languages);
            filtered = filtered.Where(t => !named.Any(c => c.SpecCode == t.SpecCode));
        }

        return filtered;
    }

    /// <summary>
    /// Tập tên bản ngữ ĐÃ DUYỆT thuộc các ngôn ngữ của một quốc gia.
    ///
    /// <para>Trả về <c>IQueryable</c> chứ không phải <c>bool</c> có chủ ý: cả danh sách lẫn
    /// thống kê đều dùng nên tách ra để hai chỗ không lệch nhau, mà nếu viết thành method
    /// trả bool rồi gọi trong lambda thì EF không dịch được method call. Hoisted ra biến
    /// trước khi vào lambda thì EF compose được thành subquery.</para>
    /// </summary>
    private IQueryable<CommonName> BuildVerifiedNameQuery(string[] languages)
    {
        var names = db.CommonNames.AsNoTracking();
        names = names.Where(c => c.IsVerified);
        names = names.Where(c => c.Language != null);
        names = names.Where(c => languages.Contains(c.Language!));

        return names;
    }
}