using FishDex.EFCore.Entity.Species;
using FishDex.EFCore.Repository.BaseGeneric;

namespace FishDex.EFCore.Repository.Interface;

public interface ISpeciesRepository : IGenericRepository<Species>
{
    Task<(IReadOnlyList<Species> Items, int TotalCount)> SearchWithCountAsync(
        string? query, Guid? famId, int? genusCode, string? language,
        int page, int pageSize, CancellationToken ct = default);
}

