using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using FishDex.Domain.DTOs.Media;
using FishDex.Domain.Services.Interfaces;
using FishDex.Domain.Services.MediaAccess;
using FishDex.Domain.Settings;
using FishDex.EFCore.Repository.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FishDex.Domain.Services;

/// <summary>
/// Ký + redeem link media-access. Port từ grapeseed-student-service (MediaAccessSignatureService +
/// MediaAccessLinkService + RedeemMediaAccessHandler), gọn lại cho FishLover:
/// <list type="bullet">
/// <item>Bỏ country/tenant — chuỗi ký chỉ còn purpose, resource, exp.</item>
/// <item>Chỉ có SAS dùng chung (resource = "/"). Link 1 path chưa cần nên chưa làm; <c>shared=1</c> vẫn giữ
/// trong URL để sau này thêm được mà không đổi format.</item>
/// </list>
/// </summary>
/// <remarks>
/// Redeem chạy cho khách ẩn danh nên chỉ tin chữ ký HMAC, và check chữ ký TRƯỚC mọi rule nghiệp vụ.
/// Link thật nhưng hết hạn có mã riêng (FE xin SAS mới); mọi lý do khác trả cùng một 403.
/// <para>SAS ẩn danh mở được mọi key trong bucket, nên key nào do user upload (prefix <c>community/</c>,
/// lại đoán được vì specCode tăng dần) phải qua kiểm duyệt mới redeem được — xem
/// <see cref="IsPublicCommunityImageAsync"/>.</para>
/// </remarks>
public class MediaAccessService : IMediaAccessService
{
    // Domain separation: chữ ký ở đây không bao giờ verify được cho giao thức khác dùng chung khoá.
    // Đổi ý nghĩa field mà giữ layout thì phải thêm hậu tố ("-v2") để link cũ chết.
    private const string PurposeLabel = "fishlover-media-access";

    private const string SharedResource = "/";
    private const string SharedFlag = "1";

    /// <summary>Ảnh user upload cho loài community: <c>community/{specCode}/cover{ext}</c>.</summary>
    private const string CommunityKeyPrefix = "community/";

    // 32 byte → 43 ký tự base64url không padding.
    private const int SignatureLength = 43;

    /// <summary>Lệch đồng hồ giữa lúc ký và lúc redeem mà trần thời hạn chấp nhận.</summary>
    private const int ExpiryCeilingSkewInSeconds = 60;

    /// <summary>Redirect được cache tới trước khi presigned URL hết hạn bấy nhiêu giây.</summary>
    private const int CacheMarginInSeconds = 60;

    // Đúng 1 cách viết số mà issuer phát ra (không dấu, không số 0 đầu) → số viết lại không alias được chữ ký.
    private static readonly Regex ExpiryPattern = new("^[1-9][0-9]{0,18}$", RegexOptions.Compiled);

    private readonly byte[]? _key;
    private readonly MediaAccessSettings _settings;
    private readonly IStorageService _storage;
    private readonly TimeProvider _time;
    private readonly IServiceScopeFactory _scopeFactory;

    public MediaAccessService(
        IOptions<MediaAccessSettings> options,
        IStorageService storage,
        TimeProvider time,
        IServiceScopeFactory scopeFactory,
        ILogger<MediaAccessService> logger)
    {
        _settings = options.Value;
        _storage = storage;
        _time = time;
        _scopeFactory = scopeFactory;
        _key = DecodeKey(_settings.Key, logger);
    }

    public SharedMediaAccessSignatureDto? CreateSharedSignature(int? expiryInSeconds)
    {
        if (_key is null) return null;

        var expiry = expiryInSeconds ?? Math.Min(_settings.DefaultExpiryInSeconds, _settings.MaxExpiryInSeconds);
        if (expiry < 1 || expiry > _settings.MaxExpiryInSeconds)
            throw new ArgumentOutOfRangeException(nameof(expiryInSeconds),
                $"expiryInSeconds phải trong khoảng 1–{_settings.MaxExpiryInSeconds}.");

        var expiresAt = _time.GetUtcNow().ToUnixTimeSeconds() + expiry;
        var signature = ToBase64Url(ComputeHash(SharedResource, expiresAt));

        // Thứ tự param cố định → cùng một grant luôn ra cùng một URL (browser cache trúng).
        var sas = $"shared={SharedFlag}&exp={expiresAt.ToString(CultureInfo.InvariantCulture)}&sig={signature}";
        return new SharedMediaAccessSignatureDto(sas, DateTimeOffset.FromUnixTimeSeconds(expiresAt));
    }

    public async Task<MediaAccessRedeemResult> RedeemAsync(string? path, string? shared, string? exp, string? sig, CancellationToken ct = default)
    {
        if (_key is null)
            return MediaAccessRedeemResult.Deny(MediaAccessDenial.NotConfigured);

        // Cờ shared không được ký nên chỉ nhận đúng cách viết issuer phát ra.
        if (shared != SharedFlag
            || exp is null || !ExpiryPattern.IsMatch(exp)
            || !long.TryParse(exp, NumberStyles.None, CultureInfo.InvariantCulture, out var expiresAt)
            || !AssetKeyCanonicalizer.TryCanonicalizePath(path, out var objectKey))
            return MediaAccessRedeemResult.Deny(MediaAccessDenial.Invalid);

        var provided = new byte[HMACSHA256.HashSizeInBytes];
        if (!TryFromBase64Url(sig, provided))
            return MediaAccessRedeemResult.Deny(MediaAccessDenial.Invalid);

        // So sánh constant-time để thời gian phản hồi không lộ chữ ký giả khớp được bao nhiêu byte.
        if (!CryptographicOperations.FixedTimeEquals(ComputeHash(SharedResource, expiresAt), provided))
            return MediaAccessRedeemResult.Deny(MediaAccessDenial.Invalid);

        var remainingSeconds = expiresAt - _time.GetUtcNow().ToUnixTimeSeconds();
        if (remainingSeconds <= 0)
            return MediaAccessRedeemResult.Deny(MediaAccessDenial.Expired);

        // Áp lại trần thời hạn: hạ MaxExpiry trong config thì link đã phát cũng bị rút ngắn theo.
        if (remainingSeconds > _settings.MaxExpiryInSeconds + ExpiryCeilingSkewInSeconds)
            return MediaAccessRedeemResult.Deny(MediaAccessDenial.Expired);

        if (objectKey.StartsWith(CommunityKeyPrefix, StringComparison.Ordinal)
            && !await IsPublicCommunityImageAsync(objectKey, ct))
            return MediaAccessRedeemResult.Deny(MediaAccessDenial.Invalid);

        // Presigned URL không bao giờ sống lâu hơn link sinh ra nó.
        var lifetime = (int)Math.Min(_settings.RedirectExpiryInSeconds, remainingSeconds);
        var location = await _storage.GetPresignedUrlAsync(objectKey, TimeSpan.FromSeconds(lifetime), ct);
        if (location is null)
            return MediaAccessRedeemResult.Deny(MediaAccessDenial.NotConfigured);

        return new MediaAccessRedeemResult(location, Math.Max(0, lifetime - CacheMarginInSeconds), null);
    }

    /// <summary>
    /// Ảnh community chỉ public khi loài đã được admin duyệt VÀ key đúng là ảnh bìa hiện tại của loài đó.
    /// Ảnh loài chờ duyệt / bị từ chối / ảnh bìa cũ đều bị từ chối — trước media-access chỉ chủ loài và
    /// admin mới có presigned URL của chúng. Browser cache 302 nên mỗi ảnh chỉ tốn 1 query / ~14 phút.
    /// </summary>
    private async Task<bool> IsPublicCommunityImageAsync(string objectKey, CancellationToken ct)
    {
        var specCodeSegment = objectKey.Split('/')[1];
        if (!int.TryParse(specCodeSegment, NumberStyles.None, CultureInfo.InvariantCulture, out var specCode))
            return false;

        // Service là singleton, repository dùng DbContext scoped → mở scope riêng cho lần tra này.
        await using var scope = _scopeFactory.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<ICommunitySpeciesRepository>();
        var species = await repo.GetVerifiedByCodeAsync(specCode, ct);

        return species?.ThumbnailObjectKey == objectKey;
    }

    private byte[] ComputeHash(string resource, long expiresAt)
    {
        var stringToSign = string.Join('\n',
            PurposeLabel,
            resource,
            expiresAt.ToString(CultureInfo.InvariantCulture));

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
            if (bytes.Length >= MediaAccessSettings.MinKeyLengthInBytes) return bytes;

            logger.LogError("MediaAccess:Key chỉ có {Length} byte, cần ≥ {Min} — media-access bị tắt.",
                bytes.Length, MediaAccessSettings.MinKeyLengthInBytes);
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
            return false;

        // Khôi phục base64 chuẩn: đổi lại bảng chữ cái + đúng 1 dấu '=' mà giá trị 32 byte cần.
        var base64 = value.Replace('-', '+').Replace('_', '/') + "=";
        return Convert.TryFromBase64Chars(base64, destination, out var written)
               && written == destination.Length;
    }

    private static bool IsBase64UrlChar(char c)
    {
        return c is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or (>= '0' and <= '9') or '-' or '_';
    }
}
