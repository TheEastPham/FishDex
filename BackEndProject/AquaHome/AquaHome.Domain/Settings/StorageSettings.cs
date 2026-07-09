namespace AquaHome.Domain.Settings;

public class StorageSettings
{
    public const string SectionName = "Storage";

    /// <summary>r2 | minio | s3</summary>
    public string Provider { get; set; } = "r2";

    /// <summary>R2: https://&lt;accountId&gt;.r2.cloudflarestorage.com  |  MinIO: http://localhost:9000  |  S3: leave empty</summary>
    public string? ServiceUrl { get; set; }

    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>Bucket dành riêng cho AquaHome — ví dụ: aquahome</summary>
    public string BucketName { get; set; } = "aquahome";

    /// <summary>dev | staging | prod — nhúng vào object key để phân tách môi trường</summary>
    public string Environment { get; set; } = "dev";

    /// <summary>Expiry của presigned GET URL (phút).</summary>
    public int PresignedUrlExpiryMinutes { get; set; } = 60;

    /// <summary>Expiry của presigned PUT URL (phút). Ngắn hơn GET.</summary>
    public int PresignedPutUrlExpiryMinutes { get; set; } = 5;

    /// <summary>True nếu MinIO chạy local không có HTTPS.</summary>
    public bool ForcePathStyle { get; set; } = true;
}
