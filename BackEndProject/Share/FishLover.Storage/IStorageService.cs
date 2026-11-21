namespace FishLover.Storage;

/// <summary>
/// Object storage của một bucket (R2 / MinIO / S3). Mỗi service có một bucket riêng, cấu hình ở section <c>Storage</c>.
/// </summary>
/// <remarks>
/// <para><b>Chưa cấu hình</b> (thiếu <c>Storage:AccessKey</c>): các hàm đọc/ký chạy rỗng — trả null, không xoá gì —
/// để local dev chạy được không cần R2. Riêng <see cref="PutObjectAsync"/> NÉM, vì ghi hụt là mất dữ liệu.</para>
/// <para><b>Key</b> do key policy của từng service sinh ra (FishDex: <c>SystemImage.ObjectKey</c>,
/// <c>FishDexObjectKeys</c>; AquaHome: <c>AquaHomeObjectKeys</c>) — không tự ghép chuỗi ở nơi gọi.</para>
/// </remarks>
public interface IStorageService
{
    /// <summary>Có credential chưa.</summary>
    bool IsConfigured { get; }

    /// <summary>Presigned GET với thời hạn mặc định (<c>Storage:PresignedUrlExpiryMinutes</c>). Null nếu chưa cấu hình hoặc ký lỗi.</summary>
    Task<string?> GetPresignedUrlAsync(string objectKey, CancellationToken ct = default);

    /// <summary>Presigned GET với thời hạn tự chọn — media-access dùng để URL không sống lâu hơn link gốc.</summary>
    Task<string?> GetPresignedUrlAsync(string objectKey, TimeSpan expiry, CancellationToken ct = default);

    /// <summary>
    /// Presigned PUT để client upload thẳng lên bucket (thời hạn <c>Storage:PresignedPutUrlExpiryMinutes</c>).
    /// Null nếu chưa cấu hình hoặc ký lỗi.
    /// </summary>
    /// <remarks>
    /// URL này KHÔNG chặn được dung lượng: R2 không có POST policy, và header giới hạn không ký được vì browser
    /// không gửi. Luồng nào phát URL phải gọi <see cref="GetObjectSizeAsync"/> ở bước confirm rồi mới tin file.
    /// </remarks>
    Task<string?> GeneratePresignedPutUrlAsync(string objectKey, string contentType, CancellationToken ct = default);

    /// <summary>
    /// Dung lượng thật của object (HEAD). Null nếu object không tồn tại hoặc storage chưa cấu hình; lỗi khác thì NÉM —
    /// người gọi phân biệt được "chưa upload" với "R2 trục trặc".
    /// </summary>
    Task<long?> GetObjectSizeAsync(string objectKey, CancellationToken ct = default);

    /// <summary>Ghi object từ BE. Lỗi hoặc chưa cấu hình thì NÉM.</summary>
    Task PutObjectAsync(string objectKey, byte[] content, string contentType, CancellationToken ct = default);

    /// <summary>Xoá object. Lỗi chỉ ghi log — object mồ côi dọn sau được.</summary>
    Task DeleteAsync(string objectKey, CancellationToken ct = default);

    /// <summary>Xoá mọi object có key bắt đầu bằng <paramref name="prefix"/>. Prefix rỗng thì không làm gì. Lỗi chỉ ghi log.</summary>
    Task DeleteByPrefixAsync(string prefix, CancellationToken ct = default);
}
