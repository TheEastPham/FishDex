using System.Collections.Concurrent;

namespace FishLover.Storage;

/// <summary>
/// Adapter thứ hai: bucket trong bộ nhớ, cho test (và chạy local không có R2). Cùng hợp đồng với
/// <see cref="S3StorageService"/>, kể cả hành vi khi chưa cấu hình (<c>configured: false</c>).
/// </summary>
/// <remarks>
/// URL "presigned" có dạng <c>memory://{key}</c> — so được trong test qua <see cref="UrlFor"/>. Thời điểm ghi lấy từ
/// <c>time</c> (mặc định đồng hồ hệ thống) để test job dọn dẹp giả lập được object cũ.
/// </remarks>
public sealed class InMemoryStorageService(bool configured = true, TimeProvider? time = null) : IStorageService
{
    private readonly ConcurrentDictionary<string, StoredObject> _objects = new(StringComparer.Ordinal);
    private readonly TimeProvider _time = time ?? TimeProvider.System;

    public sealed record StoredObject(byte[] Content, string ContentType, DateTimeOffset LastModified);

    public bool IsConfigured => configured;

    /// <summary>Các object đang có — để test kiểm tra.</summary>
    public IReadOnlyDictionary<string, StoredObject> Objects => _objects;

    public static string UrlFor(string objectKey)
    {
        return $"memory://{objectKey}";
    }

    public Task<string?> GetPresignedUrlAsync(string objectKey, CancellationToken ct = default)
    {
        return Task.FromResult<string?>(configured ? UrlFor(objectKey) : null);
    }

    public Task<string?> GetPresignedUrlAsync(string objectKey, TimeSpan expiry, CancellationToken ct = default)
    {
        return GetPresignedUrlAsync(objectKey, ct);
    }

    public Task<string?> GeneratePresignedPutUrlAsync(string objectKey, string contentType, CancellationToken ct = default)
    {
        return Task.FromResult<string?>(configured ? UrlFor(objectKey) + "?upload" : null);
    }

    public Task<long?> GetObjectSizeAsync(string objectKey, CancellationToken ct = default)
    {
        if (configured && _objects.TryGetValue(objectKey, out var stored))
        {
            return Task.FromResult<long?>(stored.Content.LongLength);
        }

        return Task.FromResult<long?>(null);
    }

    public Task PutObjectAsync(string objectKey, byte[] content, string contentType, CancellationToken ct = default)
    {
        if (!configured)
        {
            throw new InvalidOperationException("Storage chưa cấu hình — không ghi được object.");
        }

        _objects[objectKey] = new StoredObject(content.ToArray(), contentType, _time.GetUtcNow());
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string objectKey, CancellationToken ct = default)
    {
        if (configured)
        {
            _objects.TryRemove(objectKey, out _);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<StoredObjectInfo>> ListAsync(string prefix, CancellationToken ct = default)
    {
        if (!configured || string.IsNullOrWhiteSpace(prefix))
        {
            return Task.FromResult<IReadOnlyList<StoredObjectInfo>>([]);
        }

        IReadOnlyList<StoredObjectInfo> result = _objects
            .Where(o => o.Key.StartsWith(prefix, StringComparison.Ordinal))
            .OrderBy(o => o.Key, StringComparer.Ordinal)
            .Select(o => new StoredObjectInfo(o.Key, o.Value.Content.LongLength, o.Value.LastModified))
            .ToList();
        return Task.FromResult(result);
    }

    public Task DeleteByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        if (!configured || string.IsNullOrWhiteSpace(prefix))
        {
            return Task.CompletedTask;
        }

        foreach (var key in _objects.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)))
        {
            _objects.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }
}
