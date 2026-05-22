using AutoMapper;
using FishDex.Domain.DTOs.Media;
using FishDex.Domain.Services.Interfaces;
using FishDex.EFCore.Repository.Interface;

namespace FishDex.Domain.Services;

public class MediaService(
    ISystemImageRepository imageRepo,
    IMapper mapper) : IMediaService
{
    public async Task<IReadOnlyList<SystemImageDto>> GetBySpecCodeAsync(int specCode, CancellationToken ct = default)
    {
        var items = await imageRepo.FindAsync(i => i.SpecCode == specCode);
        return mapper.Map<List<SystemImageDto>>(items.ToList());
    }

    public async Task<SystemImageDto?> GetPreferredImageAsync(int specCode, CancellationToken ct = default)
    {
        var items = await imageRepo.FindAsync(i => i.SpecCode == specCode && i.PicPreferred == true);
        var entity = items.FirstOrDefault();
        return entity is null ? null : mapper.Map<SystemImageDto>(entity);
    }
}
