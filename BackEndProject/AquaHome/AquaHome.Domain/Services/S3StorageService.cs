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
            config.ServiceURL          = _settings.ServiceUrl;
            config.AuthenticationRegion = "apac";
            config.UseHttp             = _settings.ServiceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
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

            // Enforce max upload size so oversized files get rejected by MinIO directly
            request.Headers["x-amz-content-sha256"] = "UNSIGNED-PAYLOAD";
            request.Metadata.Add("x-amz-meta-max-bytes", maxBytes.ToString());

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
}
