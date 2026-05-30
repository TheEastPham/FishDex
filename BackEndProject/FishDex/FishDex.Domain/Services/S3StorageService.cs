using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using FishDex.Domain.Services.Interfaces;
using FishDex.Domain.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FishDex.Domain.Services;

public class S3StorageService : IStorageService
{
    private readonly IAmazonS3 _s3;
    private readonly StorageSettings _settings;
    private readonly ILogger<S3StorageService> _logger;

    public S3StorageService(IOptions<StorageSettings> options, ILogger<S3StorageService> logger)
    {
        _settings = options.Value;
        _logger = logger;

        var credentials = new BasicAWSCredentials(_settings.AccessKey, _settings.SecretKey);

        var config = new AmazonS3Config
        {
            ForcePathStyle = _settings.ForcePathStyle,
        };

        // MinIO / R2 — custom endpoint; S3 — region-based
        if (!string.IsNullOrEmpty(_settings.ServiceUrl))
            config.ServiceURL = _settings.ServiceUrl;
        else
            config.RegionEndpoint = RegionEndpoint.APSoutheast1;

        _s3 = new AmazonS3Client(credentials, config);
    }

    public async Task<string?> GetPresignedUrlAsync(string objectKey, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_settings.AccessKey))
        {
            _logger.LogDebug("Storage not configured — returning null for {Key}", objectKey);
            return null;
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

            // AWS SDK luôn generate https:// — fix lại về http:// nếu endpoint là HTTP (local MinIO)
            if (_settings.ServiceUrl?.StartsWith("http://") == true)
                url = url.Replace("https://", "http://", StringComparison.Ordinal);

            return await Task.FromResult(url);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate presigned URL for {Key}", objectKey);
            return null;
        }
    }
}
