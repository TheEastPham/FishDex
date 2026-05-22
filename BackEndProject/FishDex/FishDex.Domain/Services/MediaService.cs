using FishDex.Domain.DTOs.Media;
using FishDex.Domain.Mappings;
using FishDex.Domain.Services.Interfaces;
using FishDex.EFCore.Repository.Interface;

namespace FishDex.Domain.Services;

public class MediaService(
    ISystemImageRepository imageRepo) : IMediaService
{
    public async Task<IReadOnlyList<SystemImageDto>> GetBySpecCodeAsync(int specCode, CancellationToken ct = default)
    {
        var items = await imageRepo.FindAsync(i => i.SpecCode == specCode);
        return items.Select(i => i.ToDto()).ToList();
    }

    public async Task<SystemImageDto?> GetPreferredImageAsync(int specCode, CancellationToken ct = default)
    {
        var items = await imageRepo.FindAsync(i => i.SpecCode == specCode && i.PicPreferred == true);
        return items.FirstOrDefault()?.ToDto();
    }
}
