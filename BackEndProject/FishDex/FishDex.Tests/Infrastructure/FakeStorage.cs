using FishDex.Domain.Services.Interfaces;

namespace FishDex.Tests.Infrastructure;

/// <summary>
/// Adapter thứ hai của IStorageService: "ký" URL bằng tiền tố cố định để test so được.
/// <c>configured: false</c> mô phỏng storage chưa cấu hình (S3StorageService trả null).
/// </summary>
public sealed class FakeStorage(bool configured = true) : IStorageService
{
    public static string Url(string objectKey)
    {
        return $"signed://{objectKey}";
    }

    public Task<string?> GetPresignedUrlAsync(string objectKey, CancellationToken ct = default)
    {
        return Task.FromResult<string?>(configured ? Url(objectKey) : null);
    }

    public Task<string?> GetPresignedUrlAsync(string objectKey, TimeSpan expiry, CancellationToken ct = default)
    {
        return GetPresignedUrlAsync(objectKey, ct);
    }

    public Task<string?> GeneratePresignedPutUrlAsync(string objectKey, string contentType, long maxBytes, CancellationToken ct = default)
    {
        return Task.FromResult<string?>(null);
    }

    public Task DeleteAsync(string objectKey, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}
