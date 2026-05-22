using FishDex.Domain.DTOs.Media;

namespace FishDex.Domain.Services.Interfaces;

public interface IMediaService
{
    Task<IReadOnlyList<SystemImageDto>> GetBySpecCodeAsync(int specCode, CancellationToken ct = default);
    Task<SystemImageDto?> GetPreferredImageAsync(int specCode, CancellationToken ct = default);
}
