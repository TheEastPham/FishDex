namespace AquaHome.Domain.Services.Interfaces;

public interface IStorageService
{
    /// <summary>Presigned PUT URL để FE upload thẳng lên MinIO. Giới hạn size tối đa maxBytes.</summary>
    Task<string?> GeneratePresignedPutUrlAsync(string objectKey, string contentType, long maxBytes, CancellationToken ct = default);

    /// <summary>Presigned GET URL để FE đọc ảnh.</summary>
    Task<string?> GetPresignedUrlAsync(string objectKey, CancellationToken ct = default);

    /// <summary>Xóa object khỏi storage.</summary>
    Task DeleteAsync(string objectKey, CancellationToken ct = default);
}
