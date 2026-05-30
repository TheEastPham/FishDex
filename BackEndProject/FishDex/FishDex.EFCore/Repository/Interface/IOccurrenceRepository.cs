using FishDex.EFCore.Entity.Occurrence;
using FishDex.EFCore.Repository.BaseGeneric;

namespace FishDex.EFCore.Repository.Interface;

public interface IOccurrenceRepository : IGenericRepository<Occurrence>
{
    Task<IReadOnlyList<string>> GetDistinctCountryCodesAsync(int specCode, CancellationToken ct = default);
}

