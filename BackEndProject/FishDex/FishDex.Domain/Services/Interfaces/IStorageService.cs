namespace FishDex.Domain.Services.Interfaces;

public interface IStorageService
{
    /// <summary>
    /// Trả về presigned URL cho object. Null nếu storage chưa được cấu hình.
    /// </summary>
    Task<string?> GetPresignedUrlAsync(string objectKey, CancellationToken ct = default);
}
