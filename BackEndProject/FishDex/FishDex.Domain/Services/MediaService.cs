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

        var result = new List<SystemImageDto>(items.Count());
        foreach (var i in items)
        {
            var dto = i.ToDto();
            var url = await storage.GetPresignedUrlAsync(i.Name, ct);
            result.Add(dto with { Url = url });
        }
        return result;
    }

    public async Task<SystemImageDto?> GetPreferredImageAsync(int specCode, CancellationToken ct = default)
    {
        var items = await imageRepo.FindAsync(i => i.SpecCode == specCode && i.PicPreferred == true);
        var entity = items.FirstOrDefault();
        if (entity is null) return null;

        var dto = entity.ToDto();
        var url = await storage.GetPresignedUrlAsync(entity.Name, ct);
        return dto with { Url = url };
    }
}
