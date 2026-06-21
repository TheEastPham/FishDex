using FishDex.EFCore.Entity.Cache;

namespace FishDex.EFCore.Cache;

/// <summary>
/// Cache-aside access for pre-flattened SpeciesSnapshot records.
/// Callers should request a snapshot; if absent the implementation populates it from FishBase tables.
/// </summary>
public interface ISpeciesCache
{
    /// <summary>
    /// Returns the cached snapshot for <paramref name="specCode"/>, or null if not found and population failed.
    /// </summary>
    Task<SpeciesSnapshot?> GetOrPopulateAsync(int specCode, CancellationToken ct = default);

    /// <summary>
    /// Returns cached snapshots for multiple specCodes. Missing entries are populated on demand.
    /// </summary>
    Task<IReadOnlyList<SpeciesSnapshot>> GetOrPopulateManyAsync(IEnumerable<int> specCodes, CancellationToken ct = default);

    /// <summary>
    /// Forces a re-population of the snapshot from FishBase tables regardless of whether one exists.
    /// </summary>
    Task<SpeciesSnapshot?> RefreshAsync(int specCode, CancellationToken ct = default);

    /// <summary>
    /// Removes the snapshot so the next call to GetOrPopulate will re-build it.
    /// </summary>
    Task InvalidateAsync(int specCode, CancellationToken ct = default);
}
