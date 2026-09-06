namespace AquaHome.Domain.Services.Interfaces;

public interface IStorageService
{
    /// <summary>Presigned PUT URL để FE upload thẳng lên MinIO. Giới hạn size tối đa maxBytes.</summary>
    Task<string?> GeneratePresignedPutUrlAsync(string objectKey, string contentType, long maxBytes, CancellationToken ct = default);

    /// <summary>Presigned GET URL để FE đọc ảnh.</summary>
    Task<string?> GetPresignedUrlAsync(string objectKey, CancellationToken ct = default);

    /// <summary>Xóa object khỏi storage.</summary>
    Task DeleteAsync(string objectKey, CancellationToken ct = default);

    /// <summary>
    /// Ghi object trực tiếp từ BE (content.json của bài viết, ảnh admin upload qua API).
    /// Khác các hàm trên: lỗi thì NÉM, không nuốt — bài viết không có nội dung trên R2 thì
    /// hàng trong DB thành rác, nên request phải fail để admin biết mà upload lại.
    /// </summary>
    Task PutObjectAsync(string objectKey, byte[] content, string contentType, CancellationToken ct = default);

    /// <summary>Xóa mọi object có key bắt đầu bằng prefix — dùng khi xóa cả folder của một bài viết.</summary>
    Task DeleteByPrefixAsync(string prefix, CancellationToken ct = default);
}
