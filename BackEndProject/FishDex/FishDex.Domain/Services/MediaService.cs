using FishDex.Domain.DTOs.Media;
using FishDex.Domain.Mappings;
using FishDex.Domain.Services.Interfaces;
using FishDex.Domain.SpeciesProfiles;
using FishDex.EFCore.Repository.Interface;
using FishLover.Storage;

namespace FishDex.Domain.Services;

public class MediaService(ISystemImageRepository imageRepo, IStorageService storage) : IMediaService
{
    public async Task<IReadOnlyList<SystemImageDto>> GetBySpecCodeAsync(int specCode, CancellationToken ct = default)
    {
        var items = await imageRepo.FindAsync(i => i.SpecCode == specCode);

        return await Task.WhenAll(items.Select(async i =>
        {
            var url = await storage.GetPresignedUrlAsync(i.ObjectKey, ct);
            return i.ToDto() with { Url = url, ObjectKey = i.ObjectKey };
        }));
    }

    /// <summary>Ảnh đại diện — cùng rule thumbnail với Species Profile (detail, search, summaries).</summary>
    public async Task<SystemImageDto?> GetPreferredImageAsync(int specCode, CancellationToken ct = default)
    {
        var entity = SpeciesRules.PickThumbnail(await imageRepo.FindAsync(i => i.SpecCode == specCode));
        if (entity is null) return null;

        var url = await storage.GetPresignedUrlAsync(entity.ObjectKey, ct);
        return entity.ToDto() with { Url = url, ObjectKey = entity.ObjectKey };
    }
}
