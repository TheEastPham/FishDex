using FishDex.EFCore.DbContexts;
using FishDex.EFCore.Entity.Cache;
using FishDex.EFCore.Entity.Market;
using FishDex.EFCore.Repository.Interface;
using Microsoft.EntityFrameworkCore;

namespace FishDex.EFCore.Repository;

public class CommunitySpeciesRepository(FishDexDbContext db) : ICommunitySpeciesRepository
{
    private const int CommunityMinSpecCode = 500_000;

    public async Task<IReadOnlyList<SimilarSpeciesName>> FindSimilarNamesAsync(
        string speciesName, double threshold = 0.4, int limit = 5, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(speciesName))
        {
            return [];
        }

        var name = speciesName.Trim();

        // Ba truy vấn nhỏ rồi gộp trong bộ nhớ, thay vì một UNION viết bằng SQL thô.
        // Mỗi truy vấn chỉ lấy tối đa `limit` dòng nên gộp ở C# là rẻ và dễ đọc hơn.
        var inFishDex = await db.Species
            .AsNoTracking()
            .Where(s => EF.Functions.TrigramsSimilarity(s.SpeciesName, name) >= threshold)
            .OrderByDescending(s => EF.Functions.TrigramsSimilarity(s.SpeciesName, name))
            .Take(limit)
            .Select(s => new SimilarSpeciesName(
                s.SpecCode,
                s.SpeciesName,
                SimilarNameSource.FishDex,
                EF.Functions.TrigramsSimilarity(s.SpeciesName, name)))
            .ToListAsync(ct);

        // Chỉ lấy loài CHƯA nạp — loài đã nạp thì đã nằm trong nhóm trên rồi.
        var inFishBase = await db.FishBaseSpeciesIndex
            .AsNoTracking()
            .Where(i => !i.IsLoaded)
            .Where(i => EF.Functions.TrigramsSimilarity(i.SpeciesName, name) >= threshold)
            .OrderByDescending(i => EF.Functions.TrigramsSimilarity(i.SpeciesName, name))
            .Take(limit)
            .Select(i => new SimilarSpeciesName(
                i.SpecCode,
                i.SpeciesName,
                SimilarNameSource.FishBase,
                EF.Functions.TrigramsSimilarity(i.SpeciesName, name)))
            .ToListAsync(ct);

        var inCommunity = await db.SpeciesSnapshots
            .AsNoTracking()
            .Where(s => s.DataSource == SnapshotDataSource.Community)
            .Where(s => EF.Functions.TrigramsSimilarity(s.SpeciesName, name) >= threshold)
            .OrderByDescending(s => EF.Functions.TrigramsSimilarity(s.SpeciesName, name))
            .Take(limit)
            .Select(s => new SimilarSpeciesName(
                s.SpecCode,
                s.SpeciesName,
                SimilarNameSource.Community,
                EF.Functions.TrigramsSimilarity(s.SpeciesName, name)))
            .ToListAsync(ct);

        return inFishDex
            .Concat(inFishBase)
            .Concat(inCommunity)
            .OrderByDescending(x => x.Score)
            .Take(limit)
            .ToList();
    }

    public async Task<int> GetNextCommunitySpecCodeAsync(CancellationToken ct = default)
    {
        var max = await db.SpeciesSnapshots
            .Where(s => s.SpecCode >= CommunityMinSpecCode)
            .Select(s => (int?)s.SpecCode)
            .MaxAsync(ct);

        return (max ?? CommunityMinSpecCode - 1) + 1;
    }

    public async Task AddAsync(SpeciesSnapshot snapshot, CancellationToken ct = default)
        => await db.SpeciesSnapshots.AddAsync(snapshot, ct);

    public void Detach(SpeciesSnapshot snapshot)
        => db.Entry(snapshot).State = EntityState.Detached;

    public void Remove(SpeciesSnapshot snapshot)
        => db.SpeciesSnapshots.Remove(snapshot);

    public async Task<SpeciesSnapshot?> GetCommunityByCodeAsync(int specCode, CancellationToken ct = default)
        => await db.SpeciesSnapshots
            .FirstOrDefaultAsync(s => s.SpecCode == specCode && s.DataSource == SnapshotDataSource.Community, ct);

    public async Task<IReadOnlyList<SpeciesSnapshot>> GetByContributorAsync(Guid userId, CancellationToken ct = default)
        => await db.SpeciesSnapshots
            .Where(s => s.DataSource == SnapshotDataSource.Community && s.ContributedBy == userId)
            .OrderByDescending(s => s.PopulatedAt)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<IReadOnlyList<SpeciesSnapshot>> GetPendingAsync(CancellationToken ct = default)
        => await db.SpeciesSnapshots
            .Where(s => s.DataSource == SnapshotDataSource.Community
                        && !s.IsVerified
                        && s.RejectionReason == null)
            .OrderBy(s => s.PopulatedAt)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<IReadOnlyList<SpeciesSnapshot>> SearchVerifiedAsync(string? query, CancellationToken ct = default)
    {
        var q = db.SpeciesSnapshots
            .Where(s => s.DataSource == SnapshotDataSource.Community && s.IsVerified);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var pattern = $"%{query.Trim()}%";
            q = q.Where(s => EF.Functions.ILike(s.SpeciesName, pattern)
                             || (s.CommonName != null && EF.Functions.ILike(s.CommonName, pattern)));
        }

        return await q.OrderBy(s => s.SpeciesName).AsNoTracking().ToListAsync(ct);
    }

    public async Task<SpeciesSnapshot?> GetVerifiedByCodeAsync(int specCode, CancellationToken ct = default)
        => await db.SpeciesSnapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SpecCode == specCode
                                      && s.DataSource == SnapshotDataSource.Community
                                      && s.IsVerified, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await db.SaveChangesAsync(ct);
}
