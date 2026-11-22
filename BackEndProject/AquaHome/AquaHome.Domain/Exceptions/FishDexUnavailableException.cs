namespace AquaHome.Domain.Exceptions;

/// <summary>Vì sao một lời gọi sang FishDex thất bại — để log/alert phân biệt được, không để người dùng thấy.</summary>
public enum FishDexFailure
{
    /// <summary>Không kết nối được (DNS, từ chối kết nối, container down).</summary>
    Unreachable,
    /// <summary>Quá thời hạn HttpClient.Timeout.</summary>
    Timeout,
    /// <summary>FishDex trả HTTP không phải 2xx (5xx = FishDex lỗi; 4xx = hai bên lệch hợp đồng/route).</summary>
    BadStatus,
    /// <summary>Trả 2xx nhưng body không đọc được theo shape mong đợi.</summary>
    InvalidResponse,
}

/// <summary>
/// FishDex không trả được dữ liệu. Khác với "FishDex trả danh sách rỗng" (loài không tồn tại) — hai trường hợp
/// này phải tách bạch, nếu không snapshot publish lúc FishDex sập sẽ lưu vĩnh viễn tên "Species #…" và mất phân bố.
/// API filter map thành HTTP 503.
/// </summary>
public class FishDexUnavailableException(FishDexFailure failure, string message, int? statusCode = null, Exception? inner = null)
    : Exception(message, inner)
{
    public FishDexFailure Failure { get; } = failure;
    public int? StatusCode { get; } = statusCode;
}
