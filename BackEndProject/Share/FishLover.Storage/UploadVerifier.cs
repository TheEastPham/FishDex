namespace FishLover.Storage;

/// <summary>Chính sách kiểm file thật sau upload — do từng nghiệp vụ truyền vào, module không giữ con số nào.</summary>
/// <param name="MaxBytes">Trần dung lượng; lớn hơn là <see cref="UploadVerdictKind.TooLarge"/>, bằng trần vẫn hợp lệ.</param>
/// <param name="ExpectedBytes">Dung lượng client đã khai (nếu nghiệp vụ bắt khớp). Null = không so.</param>
public sealed record UploadPolicy(long MaxBytes, long? ExpectedBytes = null);

public enum UploadVerdictKind
{
    /// <summary>File có trên storage và qua policy.</summary>
    Ok,

    /// <summary>Không có object (HEAD trả null) — chưa upload hoặc storage chưa cấu hình.</summary>
    NotUploaded,

    /// <summary>Vượt <see cref="UploadPolicy.MaxBytes"/>; object đã bị xoá.</summary>
    TooLarge,

    /// <summary>Khác <see cref="UploadPolicy.ExpectedBytes"/>; object đã bị xoá.</summary>
    Mismatch,
}

/// <param name="Kind">Kết quả kiểm.</param>
/// <param name="Size">Dung lượng thật; null khi <see cref="UploadVerdictKind.NotUploaded"/>.</param>
public readonly record struct UploadVerdict(UploadVerdictKind Kind, long? Size)
{
    public bool IsOk => Kind == UploadVerdictKind.Ok;
}

/// <summary>
/// Upload verification: bước tin file thật sau khi client PUT bằng presigned URL (URL không chặn được dung lượng,
/// xem <see cref="IStorageService.GeneratePresignedPutUrlAsync"/>). HEAD object, so với <see cref="UploadPolicy"/>,
/// và xoá object bị từ chối — file quá cỡ không bao giờ được để lại trên storage.
/// </summary>
/// <remarks>
/// <para>Chỉ trả verdict; phản ứng sau đó (xoá dòng DB, gỡ thumbnail, chuyển entry sang Rejected…) là việc của nơi gọi.
/// Module cũng không log — nơi gọi có ngữ cảnh nghiệp vụ.</para>
/// <para>Lỗi của <see cref="IStorageService.GetObjectSizeAsync"/> (R2 trục trặc) được ném nguyên, không nuốt:
/// nơi gọi cần phân biệt "chưa upload" với "storage lỗi".</para>
/// <para>Thứ tự kiểm: không có file → quá cỡ → lệch dung lượng khai báo.</para>
/// </remarks>
public sealed class UploadVerifier(IStorageService storage)
{
    public async Task<UploadVerdict> VerifyAsync(string objectKey, UploadPolicy policy, CancellationToken ct = default)
    {
        var size = await storage.GetObjectSizeAsync(objectKey, ct);
        if (size is null)
        {
            return new UploadVerdict(UploadVerdictKind.NotUploaded, null);
        }

        if (size > policy.MaxBytes)
        {
            await storage.DeleteAsync(objectKey, ct);
            return new UploadVerdict(UploadVerdictKind.TooLarge, size);
        }

        if (policy.ExpectedBytes is { } expected && size != expected)
        {
            await storage.DeleteAsync(objectKey, ct);
            return new UploadVerdict(UploadVerdictKind.Mismatch, size);
        }

        return new UploadVerdict(UploadVerdictKind.Ok, size);
    }
}
