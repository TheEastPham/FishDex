using AquaHome.Domain.Articles;
using AquaHome.Domain.Enums;
using AquaHome.Domain.Services;
using AquaHome.Domain.Storage;
using AquaHome.EFCore.Data;
using AquaHome.EFCore.Entity;
using AquaHome.EFCore.Repository;
using AquaHome.Tests.Infrastructure;
using FishLover.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AquaHome.Tests;

/// <summary>
/// Cách bài viết hiện ra với người đọc (list, detail, related): chọn bản dịch theo ngôn ngữ
/// (thiếu thì en → vi → bất kỳ) và khoá bài — khách chỉ đọc được bài Beginner; bài khoá vẫn thấy
/// metadata nhưng KHÔNG bao giờ nhận URL nội dung hay URL ảnh trong bài.
/// </summary>
public sealed class ArticlePublicViewTests : IDisposable
{
    private readonly AquaHomeDbContext _db = new(
        new DbContextOptionsBuilder<AquaHomeDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private readonly InMemoryStorageService _storage = new();
    private readonly AquaHomeObjectKeys _keys = new("test");

    public void Dispose() => _db.Dispose();

    private ArticleService Service() => new(
        new ArticleRepository(_db),
        _storage,
        _keys,
        new FakeUser(Guid.NewGuid()),
        new PublicArticleView(_storage),
        NullLogger<ArticleService>.Instance);

    private Article Seed(string slug, ReadingLevel level, string[] languages, string[]? tags = null, int status = 1)
    {
        var article = new Article
        {
            Slug = slug,
            Status = status,
            Type = (int)ArticleType.Setup,
            ReadingLevel = (int)level,
            Tags = [.. tags ?? []],
            CoverObjectKey = $"articles/{slug}/cover.jpg",
            PublishedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        foreach (var lang in languages)
            article.Translations.Add(new ArticleTranslation
            {
                Language = lang,
                Title = $"{slug} ({lang})",
                ContentObjectKey = $"articles/{slug}/{lang}.json",
                UpdatedAt = DateTime.UtcNow,
            });
        article.Assets.Add(new ArticleAsset { ObjectKey = $"articles/{slug}/photo.jpg", ContentType = "image/jpeg" });

        _db.Articles.Add(article);
        _db.SaveChanges();
        return article;
    }

    [Fact]
    public async Task A_guest_reads_a_beginner_article_in_full()
    {
        Seed("medaka-co-ban", ReadingLevel.Beginner, ["vi"]);

        var detail = await Service().GetBySlugAsync("medaka-co-ban", "vi", isAuthenticated: false);

        Assert.NotNull(detail);
        Assert.False(detail.RequiresAuth);
        Assert.Equal(InMemoryStorageService.UrlFor("articles/medaka-co-ban/vi.json"), detail.ContentUrl);
        Assert.Equal(InMemoryStorageService.UrlFor("articles/medaka-co-ban/photo.jpg"), Assert.Single(detail.Assets).Url);
    }

    [Fact]
    public async Task A_guest_sees_a_member_article_but_never_gets_its_content_or_image_urls()
    {
        Seed("loc-nuoc-nang-cao", ReadingLevel.Intermediate, ["vi"]);

        var detail = await Service().GetBySlugAsync("loc-nuoc-nang-cao", "vi", isAuthenticated: false);

        Assert.NotNull(detail);
        Assert.True(detail.RequiresAuth);
        Assert.Null(detail.ContentUrl);
        Assert.Empty(detail.Assets);
        // Metadata vẫn trả để khách thấy bài và có lý do đăng ký.
        Assert.Equal("loc-nuoc-nang-cao (vi)", detail.Title);
        Assert.Equal(InMemoryStorageService.UrlFor("articles/loc-nuoc-nang-cao/cover.jpg"), detail.ThumbnailUrl);
    }

    [Fact]
    public async Task A_signed_in_reader_gets_member_article_content()
    {
        Seed("benh-ca", ReadingLevel.Advanced, ["vi"]);

        var detail = await Service().GetBySlugAsync("benh-ca", "vi", isAuthenticated: true);

        Assert.NotNull(detail);
        Assert.False(detail.RequiresAuth);
        Assert.NotNull(detail.ContentUrl);
        Assert.Single(detail.Assets);
    }

    [Theory]
    [InlineData("de", new[] { "vi", "en" }, "en", "de")]  // thiếu → en trước
    [InlineData("de", new[] { "vi" }, "vi", "de")]        // không có en → vi
    [InlineData("en", new[] { "zh", "de" }, "de", "en")]  // không en, không vi → bản bất kỳ theo thứ tự chữ cái
    [InlineData("vi-VN", new[] { "en", "vi" }, "vi", "vi")] // FE gửi locale đầy đủ → lấy 2 ký tự đầu
    [InlineData("fr", new[] { "en", "vi" }, "vi", "vi")]  // ngôn ngữ không hỗ trợ → coi như vi
    [InlineData(null, new[] { "en" }, "en", "vi")]        // không gửi → vi, thiếu vi → en
    public async Task A_missing_translation_falls_back_in_a_fixed_order(
        string? requested, string[] available, string expectedShown, string expectedRequested)
    {
        Seed("bai-da-ngu", ReadingLevel.Beginner, available);

        var detail = await Service().GetBySlugAsync("bai-da-ngu", requested, isAuthenticated: false);

        Assert.NotNull(detail);
        Assert.Equal(expectedShown, detail.Language);
        Assert.Equal(expectedRequested, detail.RequestedLanguage);
        Assert.Equal(available.OrderBy(l => l), detail.AvailableLanguages);
    }

    [Fact]
    public async Task The_list_marks_member_articles_as_locked_for_guests_only()
    {
        Seed("mo", ReadingLevel.Beginner, ["vi"]);
        Seed("khoa", ReadingLevel.Intermediate, ["vi"]);
        Seed("chua-dich", ReadingLevel.Beginner, []);          // không có bản dịch nào → không hiện
        Seed("nhap", ReadingLevel.Beginner, ["vi"], status: 0); // Draft → không hiện

        var guest = await Service().GetPublishedAsync("vi", null, null, null, null, 1, 20, isAuthenticated: false);
        var member = await Service().GetPublishedAsync("vi", null, null, null, null, 1, 20, isAuthenticated: true);

        Assert.Equal(["khoa", "mo"], guest.Items.Select(i => i.Slug).OrderBy(s => s));
        Assert.True(guest.Items.Single(i => i.Slug == "khoa").RequiresAuth);
        Assert.False(guest.Items.Single(i => i.Slug == "mo").RequiresAuth);
        Assert.All(member.Items, i => Assert.False(i.RequiresAuth));
        Assert.All(guest.Items, i => Assert.NotNull(i.ThumbnailUrl));
    }

    // GetRelatedAsync không test ở đây: truy vấn ứng viên dùng phép giao mảng tag (Npgsql) mà EF InMemory
    // không dịch được. Phần trình bày của nó đi qua cùng PublicArticleView với trang danh sách.
}
