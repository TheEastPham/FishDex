namespace FishDex.Domain.Services.Interfaces;

public interface IStorageService
{
    /// <summary>
    /// Trả về presigned URL cho object. Null nếu storage chưa được cấu hình.
    /// </summary>
    Task<string?> GetPresignedUrlAsync(string objectKey, CancellationToken ct = default);

    /// <summary>
    /// Trả về presigned PUT URL để client tự upload thẳng lên storage. Null nếu chưa cấu hình.
    /// </summary>
    Task<string?> GeneratePresignedPutUrlAsync(string objectKey, string contentType, long maxBytes, CancellationToken ct = default);

    Task DeleteAsync(string objectKey, CancellationToken ct = default);
}
