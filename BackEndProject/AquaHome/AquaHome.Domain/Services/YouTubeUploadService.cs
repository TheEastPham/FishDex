using AquaHome.Domain.Exceptions;
using AquaHome.Domain.Services.Interfaces;
using AquaHome.Domain.Settings;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AquaHome.Domain.Services;

/// <summary>
/// Upload video contest lên kênh YouTube FishLover qua YouTube Data API v3.
/// Auth dùng refresh token của channel owner, lấy 1 lần qua OAuth2 web-server flow (xem YouTubeSettings.RefreshToken).
/// </summary>
public class YouTubeUploadService(
    IOptions<YouTubeSettings> settings,
    IStorageService storage,
    IHttpClientFactory httpClientFactory,
    ILogger<YouTubeUploadService> logger) : IYouTubeUploadService
{
    private const string ApplicationName = "FishLover";

    // Reason Google trả khi kênh hết quota / hết lượt upload trong ngày — chờ qua ngày (giờ Pacific) là gọi lại được.
    private static readonly HashSet<string> QuotaReasons = ["quotaExceeded", "dailyLimitExceeded", "uploadLimitExceeded"];

    private YouTubeService BuildClient()
    {
        var s = settings.Value;
        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets { ClientId = s.ClientId, ClientSecret = s.ClientSecret },
            Scopes = [YouTubeService.Scope.Youtube, YouTubeService.Scope.YoutubeUpload],
        });

        var token = new TokenResponse { RefreshToken = s.RefreshToken };
        var credential = new UserCredential(flow, "fishlover-channel-owner", token);

        return new YouTubeService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = ApplicationName,
        });
    }

    public async Task<string?> UploadUnlistedAsync(string objectKey, string title, string description, CancellationToken ct = default)
    {
        try
        {
            var downloadUrl = await storage.GetPresignedUrlAsync(objectKey, ct);
            if (downloadUrl is null)
            {
                logger.LogWarning("Cannot generate presigned GET URL for {ObjectKey}", objectKey);
                return null;
            }

            using var http = httpClientFactory.CreateClient();
            using var response = await http.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            // Ra file tạm chứ không vào RAM: video tới 500MB, MemoryStream làm container phình theo từng bài.
            // File seekable nên thư viện Google vẫn resume được khi 1 chunk lỗi; DeleteOnClose tự dọn kể cả khi lỗi.
            await using var videoFile = new FileStream(
                Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None,
                bufferSize: 81920, FileOptions.DeleteOnClose | FileOptions.Asynchronous);
            await response.Content.CopyToAsync(videoFile, ct);
            videoFile.Position = 0;

            var youtube = BuildClient();
            var video = new Video
            {
                Snippet = new VideoSnippet { Title = title, Description = description, CategoryId = "15" }, // Pets & Animals
                Status = new VideoStatus { PrivacyStatus = "unlisted" },
            };

            var request = youtube.Videos.Insert(video, "snippet,status", videoFile, "video/*");
            var uploadStatus = await request.UploadAsync(ct);

            if (uploadStatus.Status != UploadStatus.Completed)
            {
                ThrowIfQuotaExceeded(uploadStatus.Exception, objectKey);
                logger.LogError(uploadStatus.Exception, "YouTube upload failed for {ObjectKey}", objectKey);
                return null;
            }

            return request.ResponseBody?.Id;
        }
        catch (Exception ex) when (ex is not YouTubeQuotaExceededException)
        {
            ThrowIfQuotaExceeded(ex, objectKey);
            logger.LogError(ex, "YouTube upload threw for {ObjectKey}", objectKey);
            return null;
        }
    }

    private void ThrowIfQuotaExceeded(Exception? ex, string objectKey)
    {
        var reason = (ex as GoogleApiException)?.Error?.Errors?
            .Select(e => e.Reason)
            .FirstOrDefault(r => r is not null && QuotaReasons.Contains(r));
        if (reason is null) return;

        logger.LogWarning("YouTube từ chối upload {ObjectKey}: {Reason} — giữ bài để xác nhận lại sau", objectKey, reason);
        throw new YouTubeQuotaExceededException(reason);
    }

    public async Task SetPublicAsync(string youTubeVideoId, string? playlistId, CancellationToken ct = default)
    {
        var youtube = BuildClient();

        // Add playlist TRƯỚC khi set public: nếu playlistId sai/không thuộc channel thì lỗi xảy ra
        // lúc video vẫn còn Unlisted — admin bấm approve lại được. Nếu làm ngược lại, video đã public
        // trên YouTube trong khi DB vẫn UploadedDraft → lệch trạng thái, approve lại sinh item trùng.
        if (!string.IsNullOrEmpty(playlistId))
        {
            var item = new PlaylistItem
            {
                Snippet = new PlaylistItemSnippet
                {
                    PlaylistId = playlistId,
                    ResourceId = new ResourceId { Kind = "youtube#video", VideoId = youTubeVideoId },
                },
            };
            await youtube.PlaylistItems.Insert(item, "snippet").ExecuteAsync(ct);
        }
        else
        {
            logger.LogWarning(
                "Contest chưa cấu hình YouTubePlaylistId — video {VideoId} sẽ public nhưng không vào playlist nào",
                youTubeVideoId);
        }

        var video = new Video
        {
            Id = youTubeVideoId,
            Status = new VideoStatus { PrivacyStatus = "public" },
        };
        await youtube.Videos.Update(video, "status").ExecuteAsync(ct);
    }

    public async Task DeleteVideoAsync(string youTubeVideoId, CancellationToken ct = default)
    {
        var youtube = BuildClient();
        await youtube.Videos.Delete(youTubeVideoId).ExecuteAsync(ct);
    }

    public async Task<IReadOnlyList<string>> GetPlaylistVideoIdsAsync(string playlistId, CancellationToken ct = default)
    {
        var youtube = BuildClient();
        var videoIds = new List<string>();
        string? pageToken = null;

        do
        {
            var request = youtube.PlaylistItems.List("contentDetails");
            request.PlaylistId = playlistId;
            request.MaxResults = 50;
            request.PageToken = pageToken;

            var response = await request.ExecuteAsync(ct);
            videoIds.AddRange(response.Items.Select(i => i.ContentDetails.VideoId));
            pageToken = response.NextPageToken;
        } while (!string.IsNullOrEmpty(pageToken));

        return videoIds;
    }

    public async Task<IReadOnlyDictionary<string, long>> GetViewCountsAsync(IReadOnlyList<string> videoIds, CancellationToken ct = default)
    {
        var result = new Dictionary<string, long>();
        if (videoIds.Count == 0) return result;

        var youtube = BuildClient();

        foreach (var chunk in videoIds.Chunk(50)) // YouTube API giới hạn 50 id/request
        {
            var request = youtube.Videos.List("statistics");
            request.Id = string.Join(",", chunk);

            var response = await request.ExecuteAsync(ct);
            foreach (var video in response.Items)
            {
                var viewCount = video.Statistics?.ViewCount ?? 0;
                result[video.Id] = (long)viewCount;
            }
        }

        return result;
    }
}
