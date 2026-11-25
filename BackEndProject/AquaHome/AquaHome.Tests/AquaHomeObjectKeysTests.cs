using AquaHome.Domain.Storage;
using AquaHome.EFCore.Entity;

namespace AquaHome.Tests;

/// <summary>
/// DB và R2 đang giữ key đầy đủ theo các format dưới đây. Test này khoá từng format bằng chuỗi viết tay —
/// đổi policy mà làm đỏ test là đang làm mất object cũ.
/// </summary>
public sealed class AquaHomeObjectKeysTests
{
    private static readonly Guid A = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid B = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly AquaHomeObjectKeys _keys = new("prod");

    [Fact]
    public void Aquarium_media_key_format()
    {
        var media = new AquariumMedia { Id = B, AquariumId = A, FileName = "IMG_0001.HEIC.jpg" };

        Assert.Equal("aquahome/prod/aquaria/11111111-1111-1111-1111-111111111111/22222222-2222-2222-2222-222222222222.jpg",
            _keys.AquariumMedia(media));
    }

    [Fact]
    public void Aquarium_media_without_extension_has_no_suffix()
    {
        var media = new AquariumMedia { Id = B, AquariumId = A, FileName = "photo" };

        Assert.EndsWith("/22222222-2222-2222-2222-222222222222", _keys.AquariumMedia(media));
    }

    [Fact]
    public void Article_key_formats()
    {
        Assert.Equal("aquahome/prod/articles/11111111-1111-1111-1111-111111111111", _keys.ArticleFolder(A));
        Assert.Equal("aquahome/prod/articles/11111111-1111-1111-1111-111111111111/assets/22222222-2222-2222-2222-222222222222.png",
            _keys.ArticleAsset(A, B, ".png"));
        Assert.Equal("aquahome/prod/articles/11111111-1111-1111-1111-111111111111/cover.webp", _keys.ArticleCover(A, ".webp"));
        Assert.Equal("aquahome/prod/articles/11111111-1111-1111-1111-111111111111/vi/content.json", _keys.ArticleContent(A, "vi"));
    }

    [Fact]
    public void Contest_key_formats()
    {
        Assert.Equal("aquahome/prod/contests/11111111-1111-1111-1111-111111111111/22222222-2222-2222-2222-222222222222.mp4",
            _keys.ContestEntryVideo(A, B, ".mp4"));
        Assert.Equal("aquahome/prod/contests/11111111-1111-1111-1111-111111111111/prize-tiers/22222222-2222-2222-2222-222222222222.png",
            _keys.ContestPrizeTierLogo(A, B, ".png"));
        Assert.Equal("aquahome/prod/contests/11111111-1111-1111-1111-111111111111/sponsors/22222222-2222-2222-2222-222222222222.svg",
            _keys.ContestSponsorLogo(A, B, ".svg"));
    }

    [Fact]
    public void Every_key_lives_under_its_environment()
    {
        var dev = new AquaHomeObjectKeys("dev");

        Assert.StartsWith("aquahome/dev/", dev.ArticleCover(A, ".jpg"));
        Assert.StartsWith("aquahome/dev/", dev.ContestEntryVideo(A, B, ".mp4"));
    }

    // ── TryParse: nền cho media-access sau này ─────────────────────────────

    public static TheoryData<string, AquaHomeAssetKind, Guid?, string?> GeneratedKeys()
    {
        var keys = new AquaHomeObjectKeys("staging");
        return new TheoryData<string, AquaHomeAssetKind, Guid?, string?>
        {
            { keys.AquariumMedia(new AquariumMedia { Id = B, AquariumId = A, FileName = "x.jpg" }), AquaHomeAssetKind.AquariumMedia, B, null },
            { keys.ArticleAsset(A, B, ".png"), AquaHomeAssetKind.ArticleAsset, B, null },
            { keys.ArticleCover(A, ".jpg"), AquaHomeAssetKind.ArticleCover, null, null },
            { keys.ArticleContent(A, "en"), AquaHomeAssetKind.ArticleContent, null, "en" },
            { keys.ContestEntryVideo(A, B, ".mp4"), AquaHomeAssetKind.ContestEntryVideo, B, null },
            { keys.ContestPrizeTierLogo(A, B, ".png"), AquaHomeAssetKind.ContestPrizeTierLogo, B, null },
            { keys.ContestSponsorLogo(A, B, ".png"), AquaHomeAssetKind.ContestSponsorLogo, B, null },
        };
    }

    [Theory]
    [MemberData(nameof(GeneratedKeys))]
    public void Every_generated_key_parses_back_to_its_kind(string key, AquaHomeAssetKind kind, Guid? itemId, string? language)
    {
        Assert.True(AquaHomeObjectKeys.TryParse(key, out var parsed));
        Assert.Equal(kind, parsed!.Kind);
        Assert.Equal("staging", parsed.Environment);
        Assert.Equal(A, parsed.OwnerId);
        Assert.Equal(itemId, parsed.ItemId);
        Assert.Equal(language, parsed.Language);
    }

    [Theory]
    [InlineData("100/22222222-2222-2222-2222-222222222222.jpg")]                                   // key FishDex
    [InlineData("community/500001/cover.jpg")]                                                    // key FishDex
    [InlineData("aquahome/prod/aquaria/not-a-guid/22222222-2222-2222-2222-222222222222.jpg")]
    [InlineData("aquahome/prod/aquaria/11111111-1111-1111-1111-111111111111/readme.txt")]
    [InlineData("aquahome/prod/aquaria/11111111-1111-1111-1111-111111111111/x/22222222-2222-2222-2222-222222222222.jpg")]
    [InlineData("aquahome/prod/articles/11111111-1111-1111-1111-111111111111")]                   // folder, không phải object
    [InlineData("aquahome/prod/articles/11111111-1111-1111-1111-111111111111/drafts/notes.txt")]
    [InlineData("aquahome/prod/unknown/11111111-1111-1111-1111-111111111111/x.jpg")]
    [InlineData("aquahome//contests/11111111-1111-1111-1111-111111111111/22222222-2222-2222-2222-222222222222.mp4")]
    public void Unknown_keys_are_rejected(string key)
    {
        Assert.False(AquaHomeObjectKeys.TryParse(key, out var parsed));
        Assert.Null(parsed);
    }
}
