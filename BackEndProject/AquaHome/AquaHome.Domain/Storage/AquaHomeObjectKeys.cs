using AquaHome.EFCore.Entity;
using FishLover.Storage;
using Microsoft.Extensions.Options;

namespace AquaHome.Domain.Storage;

/// <summary>Loại tài sản trong bucket AquaHome — nhận ra được từ object key.</summary>
public enum AquaHomeAssetKind
{
    AquariumMedia,
    ArticleAsset,
    ArticleCover,
    ArticleContent,
    ContestEntryVideo,
    ContestPrizeTierLogo,
    ContestSponsorLogo,
}

/// <summary>
/// Key đã phân tích. <see cref="OwnerId"/> là bể / bài viết / cuộc thi chứa tài sản; <see cref="ItemId"/> là
/// media / asset / entry / tier / sponsor (null với ảnh bìa và content.json); <see cref="Language"/> chỉ có ở content.json.
/// </summary>
public sealed record AquaHomeObjectRef(AquaHomeAssetKind Kind, string Environment, Guid OwnerId, Guid? ItemId, string? Language);

/// <summary>
/// Key policy của bucket AquaHome: mọi object key được sinh — và nhận diện — ở đây, không ghép chuỗi ở nơi gọi.
/// Mọi key nằm dưới <c>aquahome/{env}/</c> để dev/staging/prod dùng chung bucket mà không đè nhau.
/// </summary>
/// <remarks>
/// Key đầy đủ đã lưu trong DB (<c>ContestEntry.VideoR2Key</c>, <c>ArticleAsset.ObjectKey</c>, <c>CoverObjectKey</c>…)
/// và trên R2 — đổi format là mất object cũ. Chỉ thêm loại mới, không sửa loại đã có (test khoá từng format).
/// <para><see cref="TryParse"/> là nền cho media-access sau này: từ key biết loại tài sản để tra quyền công khai.</para>
/// </remarks>
public sealed class AquaHomeObjectKeys
{
    private const string Root = "aquahome";

    private readonly string _environment;

    public AquaHomeObjectKeys(IOptions<StorageSettings> options)
        : this(options.Value.Environment)
    {
    }

    public AquaHomeObjectKeys(string environment)
    {
        _environment = environment;
    }

    private string Base => $"{Root}/{_environment}";

    /// <summary><c>aquahome/{env}/aquaria/{aquariumId}/{mediaId}{ext}</c> — đuôi lấy từ tên file gốc.</summary>
    public string AquariumMedia(AquariumMedia media)
    {
        return $"{Base}/aquaria/{media.AquariumId}/{media.Id}{Path.GetExtension(media.FileName)}";
    }

    /// <summary><c>aquahome/{env}/articles/{articleId}</c> — xoá bài = xoá cả prefix này.</summary>
    public string ArticleFolder(Guid articleId)
    {
        return $"{Base}/articles/{articleId}";
    }

    /// <summary><c>…/articles/{articleId}/assets/{assetId}{ext}</c></summary>
    public string ArticleAsset(Guid articleId, Guid assetId, string extension)
    {
        return $"{ArticleFolder(articleId)}/assets/{assetId}{extension}";
    }

    /// <summary><c>…/articles/{articleId}/cover{ext}</c></summary>
    public string ArticleCover(Guid articleId, string extension)
    {
        return $"{ArticleFolder(articleId)}/cover{extension}";
    }

    /// <summary><c>…/articles/{articleId}/{language}/content.json</c></summary>
    public string ArticleContent(Guid articleId, string language)
    {
        return $"{ArticleFolder(articleId)}/{language}/content.json";
    }

    /// <summary><c>aquahome/{env}/contests/{contestId}/{entryId}{ext}</c> — video dự thi.</summary>
    public string ContestEntryVideo(Guid contestId, Guid entryId, string extension)
    {
        return $"{Base}/contests/{contestId}/{entryId}{extension}";
    }

    /// <summary><c>aquahome/{env}/contests/{contestId}/prize-tiers/{tierId}{ext}</c></summary>
    public string ContestPrizeTierLogo(Guid contestId, Guid tierId, string extension)
    {
        return $"{Base}/contests/{contestId}/prize-tiers/{tierId}{extension}";
    }

    /// <summary><c>aquahome/{env}/contests/{contestId}/sponsors/{sponsorId}{ext}</c></summary>
    public string ContestSponsorLogo(Guid contestId, Guid sponsorId, string extension)
    {
        return $"{Base}/contests/{contestId}/sponsors/{sponsorId}{extension}";
    }

    /// <summary>
    /// Nhận diện key do policy này sinh ra (bất kể env). Key lạ — sai cấu trúc, id không phải Guid, segment thừa —
    /// trả false, để nơi gọi mặc định từ chối.
    /// </summary>
    public static bool TryParse(string objectKey, out AquaHomeObjectRef? asset)
    {
        asset = null;
        var s = objectKey.Split('/');
        if (s.Length < 5 || s[0] != Root || s[1].Length == 0)
        {
            return false;
        }

        var env = s[1];
        if (!Guid.TryParse(s[3], out var ownerId))
        {
            return false;
        }

        switch (s[2])
        {
            case "aquaria" when s.Length == 5 && TryParseId(s[4], out var mediaId):
                asset = new AquaHomeObjectRef(AquaHomeAssetKind.AquariumMedia, env, ownerId, mediaId, null);
                return true;

            case "articles" when s.Length == 5 && IsFile(s[4], "cover"):
                asset = new AquaHomeObjectRef(AquaHomeAssetKind.ArticleCover, env, ownerId, null, null);
                return true;

            case "articles" when s.Length == 6 && s[4] == "assets" && TryParseId(s[5], out var assetId):
                asset = new AquaHomeObjectRef(AquaHomeAssetKind.ArticleAsset, env, ownerId, assetId, null);
                return true;

            case "articles" when s.Length == 6 && s[5] == "content.json" && s[4].Length > 0:
                asset = new AquaHomeObjectRef(AquaHomeAssetKind.ArticleContent, env, ownerId, null, s[4]);
                return true;

            case "contests" when s.Length == 5 && TryParseId(s[4], out var entryId):
                asset = new AquaHomeObjectRef(AquaHomeAssetKind.ContestEntryVideo, env, ownerId, entryId, null);
                return true;

            case "contests" when s.Length == 6 && s[4] == "prize-tiers" && TryParseId(s[5], out var tierId):
                asset = new AquaHomeObjectRef(AquaHomeAssetKind.ContestPrizeTierLogo, env, ownerId, tierId, null);
                return true;

            case "contests" when s.Length == 6 && s[4] == "sponsors" && TryParseId(s[5], out var sponsorId):
                asset = new AquaHomeObjectRef(AquaHomeAssetKind.ContestSponsorLogo, env, ownerId, sponsorId, null);
                return true;

            default:
                return false;
        }
    }

    /// <summary>Segment cuối dạng <c>{guid}{ext}</c>.</summary>
    private static bool TryParseId(string fileSegment, out Guid id)
    {
        return Guid.TryParse(Path.GetFileNameWithoutExtension(fileSegment), out id);
    }

    private static bool IsFile(string fileSegment, string name)
    {
        return Path.GetFileNameWithoutExtension(fileSegment) == name;
    }
}
