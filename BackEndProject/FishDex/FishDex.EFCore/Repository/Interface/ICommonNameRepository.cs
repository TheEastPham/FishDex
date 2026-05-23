using FishDex.EFCore.Entity.Species;
using FishDex.EFCore.Repository.BaseGeneric;

namespace FishDex.EFCore.Repository.Interface;

public interface ICommonNameRepository : IGenericRepository<CommonName>
{
    Task<IReadOnlyList<(string Language, int Count)>> GetTopLanguagesAsync(int topCount, CancellationToken ct = default);
}
