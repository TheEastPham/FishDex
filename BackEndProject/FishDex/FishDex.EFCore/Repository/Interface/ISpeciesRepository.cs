using FishDex.EFCore.Entity.Species;
using FishDex.EFCore.Repository.BaseGeneric;

namespace FishDex.EFCore.Repository.Interface;

public interface ISpeciesRepository : IGenericRepository<Species>
{
    Task<(IReadOnlyList<Species> Items, int TotalCount)> SearchWithCountAsync(
        string? query, Guid? famId, int? genusCode, string? language,
        int page, int pageSize, CancellationToken ct = default);

    /// <summary>Search theo skip/take (offset-based) — dùng khi cần ghép nguồn khác (community) vào phân trang.</summary>
    Task<(IReadOnlyList<Species> Items, int TotalCount)> SearchSliceAsync(
        string? query, Guid? famId, int? genusCode, string? language,
        int skip, int take, CancellationToken ct = default);

    Task<Species?> GetWithDetailsAsync(int specCode, CancellationToken ct = default);

    Task<IReadOnlyList<Species>> GetRelatedAsync(int specCode, int? genusCode, Guid famId, int limit, CancellationToken ct = default);

    Task<IReadOnlyList<Species>> GetBySpecCodesAsync(IEnumerable<int> specCodes, CancellationToken ct = default);
}

