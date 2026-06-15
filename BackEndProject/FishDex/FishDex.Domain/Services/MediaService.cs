using System.IO;
using FishDex.Domain.DTOs.Media;
using FishDex.Domain.Mappings;
using FishDex.Domain.Services.Interfaces;
using FishDex.EFCore.Repository.Interface;

namespace FishDex.Domain.Services;

public class MediaService(
    ISystemImageRepository imageRepo,
    IStorageService storage) : IMediaService
{
    public async Task<IReadOnlyList<SystemImageDto>> GetBySpecCodeAsync(int specCode, CancellationToken ct = default)
    {
        var items = await imageRepo.FindAsync(i => i.SpecCode == specCode);

        return await Task.WhenAll(items.Select(async i =>
        {
            var url = await storage.GetPresignedUrlAsync(i.ObjectKey, ct);
            return i.ToDto() with { Url = url };
        }));
    }

    public async Task<SystemImageDto?> GetPreferredImageAsync(int specCode, CancellationToken ct = default)
    {
        var items = await imageRepo.FindAsync(i => i.SpecCode == specCode && i.PicPreferred == true);
        var entity = items.FirstOrDefault();
        if (entity is null) return null;

        var dto = entity.ToDto();
        var url = await storage.GetPresignedUrlAsync(entity.ObjectKey, ct);
        return dto with { Url = url };
    }
}
