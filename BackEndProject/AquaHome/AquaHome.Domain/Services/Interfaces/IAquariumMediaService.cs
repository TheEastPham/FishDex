using AquaHome.Domain.DTOs;

namespace AquaHome.Domain.Services.Interfaces;

public interface IAquariumMediaService
{
    /// <summary>
    /// Bước 1 của upload flow: validate ownership + giới hạn 10 ảnh,
    /// tạo DB record (Pending), trả về presigned PUT URL cho FE.
    /// </summary>
    Task<PresignedUploadDto?> RequestUploadAsync(Guid aquariumId, string fileName, string contentType, CancellationToken ct = default);

    /// <summary>
    /// Bước 2: FE đã PUT lên MinIO xong, gọi endpoint này để confirm.
    /// Trả về DTO kèm presigned GET URL.
    /// </summary>
    Task<AquariumMediaDto?> ConfirmUploadAsync(Guid aquariumId, Guid mediaId, CancellationToken ct = default);

    Task<IReadOnlyList<AquariumMediaDto>> GetMediaAsync(Guid aquariumId, CancellationToken ct = default);

    Task<bool> DeleteAsync(Guid aquariumId, Guid mediaId, CancellationToken ct = default);
}
