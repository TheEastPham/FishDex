namespace FishLover.Storage;

/// <summary>
/// Section <c>Storage</c>. Giá trị mặc định khác nhau giữa các service (bucket, thời hạn PUT) được truyền vào
/// <see cref="StorageServiceCollectionExtensions.AddObjectStorage"/>; config/env var luôn thắng mặc định.
/// </summary>
public class StorageSettings
{
    public const string SectionName = "Storage";

    /// <summary>r2 | minio | s3 — chỉ để ghi chú, adapter chọn theo <see cref="ServiceUrl"/>.</summary>
    public string Provider { get; set; } = "r2";

    /// <summary>R2: https://&lt;accountId&gt;.r2.cloudflarestorage.com  |  MinIO: http://localhost:9000  |  S3: để trống</summary>
    public string? ServiceUrl { get; set; }

    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;

    /// <summary>dev | staging | prod — AquaHome nhúng vào object key để tách môi trường trong cùng bucket.</summary>
    public string Environment { get; set; } = "dev";

    /// <summary>Thời hạn presigned GET (phút).</summary>
    public int PresignedUrlExpiryMinutes { get; set; } = 60;

    /// <summary>Thời hạn presigned PUT (phút). Ngắn hơn GET.</summary>
    public int PresignedPutUrlExpiryMinutes { get; set; } = 15;

    /// <summary>True cho MinIO local (path-style); R2 production đặt false.</summary>
    public bool ForcePathStyle { get; set; } = true;
}
