using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FishLover.Storage.MediaAccess;

/// <summary>Lý do từ chối một link media-access.</summary>
public enum MediaAccessDenial
{
    /// <summary>Link sai/bị sửa — 403 chung, không lộ check nào trượt.</summary>
    Invalid,

    /// <summary>Link thật nhưng hết hạn — FE xin SAS mới.</summary>
    Expired,

    /// <summary>Chưa cấu hình khoá ký hoặc storage — 503.</summary>
    NotConfigured,
}

/// <summary>SAS dùng chung đã ký: chuỗi query <c>shared=1&amp;exp=…&amp;sig=…</c> và thời điểm hết hạn.</summary>
public sealed record SharedMediaAccessSignature(string Sas, DateTimeOffset ExpiresAt);

/// <summary>Kết quả kiểm tra link: hoặc object key đã chuẩn hoá + số giây còn lại, hoặc lý do từ chối.</summary>
public sealed record MediaAccessVerification(string? ObjectKey, long RemainingSeconds, MediaAccessDenial? Denial)
{
    public static MediaAccessVerification Deny(MediaAccessDenial reason)
    {
        return new MediaAccessVerification(null, 0, reason);
    }
}

/// <summary>
/// Ký + kiểm tra link media-access (HMAC-SHA256). Chỉ lo phần mật mã và thời hạn — quyết định key nào được
/// công khai là việc của service dùng nó (vd FishDex chặn ảnh community chưa duyệt).
/// </summary>
/// <remarks>
/// Chuỗi ký: <c>{purpose}\n{resource}\n{exp}</c>. Đổi bất kỳ phần nào = mọi link đã phát đều chết, nên giữ nguyên
/// <see cref="PurposeLabel"/> khi chỉ refactor. Chỉ có SAS dùng chung (resource = "/"); <c>shared=1</c> vẫn nằm
/// trong URL để sau này thêm link 1 path mà không đổi format.
/// </remarks>
public sealed class MediaAccessSigner
{
    // Domain separation: chữ ký ở đây không bao giờ verify được cho giao thức khác dùng chung khoá.
    // Đổi ý nghĩa field mà giữ layout thì phải thêm hậu tố ("-v2") để link cũ chết.
    private const string PurposeLabel = "fishlover-media-access";

    private const string SharedResource = "/";
    private const string SharedFlag = "1";

    // 32 byte → 43 ký tự base64url không padding.
    private const int SignatureLength = 43;

    /// <summary>Lệch đồng hồ giữa lúc ký và lúc redeem mà trần thời hạn chấp nhận.</summary>
    private const int ExpiryCeilingSkewInSeconds = 60;

    // Đúng 1 cách viết số mà issuer phát ra (không dấu, không số 0 đầu) → số viết lại không alias được chữ ký.
    private static readonly Regex ExpiryPattern = new("^[1-9][0-9]{0,18}$", RegexOptions.Compiled);

    private readonly byte[]? _key;
    private readonly MediaAccessSettings _settings;
    private readonly TimeProvider _time;

    public MediaAccessSigner(IOptions<MediaAccessSettings> options, TimeProvider time, ILogger<MediaAccessSigner> logger)
    {
        _settings = options.Value;
        _time = time;
        _key = DecodeKey(_settings.Key, logger);
    }

    public bool IsConfigured => _key is not null;

    public MediaAccessSettings Settings => _settings;

    /// <summary>Null nếu chưa cấu hình khoá. <paramref name="expiryInSeconds"/> ngoài [1, Max] → ArgumentOutOfRangeException.</summary>
    public SharedMediaAccessSignature? CreateShared(int? expiryInSeconds)
    {
        if (_key is null)
        {
            return null;
        }

        var expiry = expiryInSeconds ?? Math.Min(_settings.DefaultExpiryInSeconds, _settings.MaxExpiryInSeconds);
        if (expiry < 1 || expiry > _settings.MaxExpiryInSeconds)
        {
            throw new ArgumentOutOfRangeException(nameof(expiryInSeconds), $"expiryInSeconds phải trong khoảng 1–{_settings.MaxExpiryInSeconds}.");
        }

        var expiresAt = _time.GetUtcNow().ToUnixTimeSeconds() + expiry;
        var signature = ToBase64Url(ComputeHash(SharedResource, expiresAt));

        // Thứ tự param cố định → cùng một grant luôn ra cùng một URL (browser cache trúng).
        var sas = $"shared={SharedFlag}&exp={expiresAt.ToString(CultureInfo.InvariantCulture)}&sig={signature}";
        return new SharedMediaAccessSignature(sas, DateTimeOffset.FromUnixTimeSeconds(expiresAt));
    }

    /// <summary>
    /// Kiểm tra chữ ký TRƯỚC mọi thứ khác. Link thật nhưng hết hạn → Expired; mọi lý do khác → Invalid.
    /// Trần thời hạn được áp lại lúc redeem: hạ MaxExpiry trong config thì link đã phát cũng bị rút ngắn theo.
    /// </summary>
    public MediaAccessVerification Verify(string? path, string? shared, string? exp, string? sig)
    {
        if (_key is null)
        {
            return MediaAccessVerification.Deny(MediaAccessDenial.NotConfigured);
        }

        // Cờ shared không được ký nên chỉ nhận đúng cách viết issuer phát ra.
        if (shared != SharedFlag
            || exp is null || !ExpiryPattern.IsMatch(exp)
            || !long.TryParse(exp, NumberStyles.None, CultureInfo.InvariantCulture, out var expiresAt)
            || !AssetKeyCanonicalizer.TryCanonicalizePath(path, out var objectKey))
        {
            return MediaAccessVerification.Deny(MediaAccessDenial.Invalid);
        }

        var provided = new byte[HMACSHA256.HashSizeInBytes];
        if (!TryFromBase64Url(sig, provided))
        {
            return MediaAccessVerification.Deny(MediaAccessDenial.Invalid);
        }

        // So sánh constant-time để thời gian phản hồi không lộ chữ ký giả khớp được bao nhiêu byte.
        if (!CryptographicOperations.FixedTimeEquals(ComputeHash(SharedResource, expiresAt), provided))
        {
            return MediaAccessVerification.Deny(MediaAccessDenial.Invalid);
        }

        var remainingSeconds = expiresAt - _time.GetUtcNow().ToUnixTimeSeconds();
        if (remainingSeconds <= 0 || remainingSeconds > _settings.MaxExpiryInSeconds + ExpiryCeilingSkewInSeconds)
        {
            return MediaAccessVerification.Deny(MediaAccessDenial.Expired);
        }

        return new MediaAccessVerification(objectKey, remainingSeconds, null);
    }

    private byte[] ComputeHash(string resource, long expiresAt)
    {
        var stringToSign = string.Join('\n', PurposeLabel, resource, expiresAt.ToString(CultureInfo.InvariantCulture));
        return HMACSHA256.HashData(_key!, Encoding.UTF8.GetBytes(stringToSign));
    }

    private static byte[]? DecodeKey(string? key, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            logger.LogWarning("MediaAccess:Key chưa cấu hình — endpoint media-access sẽ trả 503.");
            return null;
        }

        try
        {
            var bytes = Convert.FromBase64String(key);
            if (bytes.Length >= MediaAccessSettings.MinKeyLengthInBytes)
            {
                return bytes;
            }

            logger.LogError("MediaAccess:Key chỉ có {Length} byte, cần ≥ {Min} — media-access bị tắt.", bytes.Length, MediaAccessSettings.MinKeyLengthInBytes);
        }
        catch (FormatException)
        {
            logger.LogError("MediaAccess:Key không phải base64 hợp lệ — media-access bị tắt.");
        }

        return null;
    }

    private static string ToBase64Url(byte[] bytes)
    {
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static bool TryFromBase64Url(string? value, Span<byte> destination)
    {
        if (value is null || value.Length != SignatureLength || !value.All(IsBase64UrlChar))
        {
            return false;
        }

        // Khôi phục base64 chuẩn: đổi lại bảng chữ cái + đúng 1 dấu '=' mà giá trị 32 byte cần.
        var base64 = value.Replace('-', '+').Replace('_', '/') + "=";
        return Convert.TryFromBase64Chars(base64, destination, out var written) && written == destination.Length;
    }

    private static bool IsBase64UrlChar(char c)
    {
        return c is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or (>= '0' and <= '9') or '-' or '_';
    }
}
