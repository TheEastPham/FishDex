using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using AquaHome.Domain.Services.Interfaces;
using AquaHome.Domain.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AquaHome.Domain.Services;

public class S3StorageService : IStorageService
{
    private readonly IAmazonS3 _s3;
    private readonly StorageSettings _settings;
    private readonly ILogger<S3StorageService> _logger;

    public S3StorageService(IOptions<StorageSettings> options, ILogger<S3StorageService> logger)
    {
        _settings = options.Value;
        _logger   = logger;

        var credentials = new BasicAWSCredentials(_settings.AccessKey, _settings.SecretKey);
        var config = new AmazonS3Config
        {
            ForcePathStyle   = _settings.ForcePathStyle,
            SignatureVersion = "4",
        };

        if (!string.IsNullOrEmpty(_settings.ServiceUrl))
        {
            config.ServiceURL           = _settings.ServiceUrl;
            config.AuthenticationRegion = "apac";
            config.UseHttp              = _settings.ServiceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
        }
        else
            config.RegionEndpoint = RegionEndpoint.APSoutheast1;

        _s3 = new AmazonS3Client(credentials, config);
    }

    public Task<string?> GeneratePresignedPutUrlAsync(
        string objectKey, string contentType, long maxBytes, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_settings.AccessKey))
        {
            _logger.LogDebug("Storage not configured — skipping presigned PUT for {Key}", objectKey);
            return Task.FromResult<string?>(null);
        }

        try
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName  = _settings.BucketName,
                Key         = objectKey,
                Expires     = DateTime.UtcNow.AddMinutes(_settings.PresignedPutUrlExpiryMinutes),
                Verb        = HttpVerb.PUT,
                ContentType = contentType,
            };

            // UNSIGNED-PAYLOAD tells R2 to skip body hash verification (required for browser PUT)
            // x-amz-meta-max-bytes is intentionally NOT signed — signing metadata forces FE to send
            // that header on PUT, but browsers don't, causing R2 signature mismatch → 403.
            // Max-size enforcement is done at the controller layer before issuing the URL.
            request.Headers["x-amz-content-sha256"] = "UNSIGNED-PAYLOAD";

            var url = _s3.GetPreSignedURL(request);
            return Task.FromResult<string?>(url);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate presigned PUT URL for {Key}", objectKey);
            return Task.FromResult<string?>(null);
        }
    }

    public Task<string?> GetPresignedUrlAsync(string objectKey, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_settings.AccessKey))
        {
            _logger.LogDebug("Storage not configured — returning null for {Key}", objectKey);
            return Task.FromResult<string?>(null);
        }

        try
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _settings.BucketName,
                Key        = objectKey,
                Expires    = DateTime.UtcNow.AddMinutes(_settings.PresignedUrlExpiryMinutes),
                Verb       = HttpVerb.GET,
            };

            var url = _s3.GetPreSignedURL(request);
            return Task.FromResult<string?>(url);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate presigned GET URL for {Key}", objectKey);
            return Task.FromResult<string?>(null);
        }
    }

    public async Task DeleteAsync(string objectKey, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_settings.AccessKey))
            return;

        try
        {
            await _s3.DeleteObjectAsync(_settings.BucketName, objectKey, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete object {Key}", objectKey);
        }
    }

    public async Task PutObjectAsync(
        string objectKey, byte[] content, string contentType, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_settings.AccessKey))
            throw new InvalidOperationException(
                "Storage chưa cấu hình (Storage:AccessKey rỗng) — không ghi được object lên R2.");

        using var stream = new MemoryStream(content, writable: false);
        var request = new PutObjectRequest
        {
            BucketName  = _settings.BucketName,
            Key         = objectKey,
            InputStream = stream,
            ContentType = contentType,

            // R2 không cài đặt STREAMING-AWS4-HMAC-SHA256-PAYLOAD-TRAILER: mặc định SDK gửi body
            // theo aws-chunked kèm checksum ở trailer và R2 trả 501. Ba công tắc dưới ép SDK gửi
            // body nguyên khối với UNSIGNED-PAYLOAD — đúng kiểu presigned PUT ở trên đang dùng.
            UseChunkEncoding                = false,
            DisablePayloadSigning           = true,
            DisableDefaultChecksumValidation = true,
        };

        try
        {
            await _s3.PutObjectAsync(request, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to put object {Key} ({Bytes} bytes)", objectKey, content.Length);
            throw;
        }
    }

    public async Task DeleteByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_settings.AccessKey) || string.IsNullOrWhiteSpace(prefix))
            return;

        try
        {
            string? continuationToken = null;
            do
            {
                var listed = await _s3.ListObjectsV2Async(new ListObjectsV2Request
                {
                    BucketName        = _settings.BucketName,
                    Prefix            = prefix,
                    ContinuationToken = continuationToken,
                }, ct);

                if (listed.S3Objects.Count > 0)
                {
                    await _s3.DeleteObjectsAsync(new DeleteObjectsRequest
                    {
                        BucketName = _settings.BucketName,
                        Objects    = listed.S3Objects.Select(o => new KeyVersion { Key = o.Key }).ToList(),
                    }, ct);
                }

                continuationToken = listed.IsTruncated == true ? listed.NextContinuationToken : null;
            } while (continuationToken is not null);
        }
        catch (Exception ex)
        {
            // Không ném: hàng DB đã xóa xong, còn object mồ côi trên R2 thì dọn sau vẫn được.
            _logger.LogWarning(ex, "Failed to delete objects under prefix {Prefix}", prefix);
        }
    }
}
