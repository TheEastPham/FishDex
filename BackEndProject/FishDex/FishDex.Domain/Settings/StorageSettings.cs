namespace FishDex.Domain.Settings;

public class StorageSettings
{
    public const string SectionName = "Storage";

    /// <summary>minio | s3 | r2</summary>
    public string Provider { get; set; } = "minio";

    /// <summary>MinIO: http://localhost:9000  |  R2: https://&lt;accountId&gt;.r2.cloudflarestorage.com  |  S3: leave empty</summary>
    public string? ServiceUrl { get; set; }

    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string BucketName { get; set; } = "fish-images";

    /// <summary>Expiry của presigned URL tính theo phút.</summary>
    public int PresignedUrlExpiryMinutes { get; set; } = 60;

    /// <summary>True nếu MinIO chạy local không có HTTPS.</summary>
    public bool ForcePathStyle { get; set; } = true;
}
