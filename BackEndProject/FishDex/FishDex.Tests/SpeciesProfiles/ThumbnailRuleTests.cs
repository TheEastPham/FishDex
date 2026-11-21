using FishDex.Domain.DTOs.Species;
using FishDex.Domain.Services;
using FishDex.EFCore.Repository;
using FishDex.Tests.Infrastructure;
using FishLover.Storage;

namespace FishDex.Tests.SpeciesProfiles;

/// <summary>
/// Thumbnail là rule dễ lệch nhất trước refactor (summaries có fallback, detail/search/related
/// thì không). Test ở đây đi qua MỌI đường đọc có hiển thị ảnh đại diện.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class ThumbnailRuleTests(PostgresFixture pg) : SpeciesProfileTestBase(pg)
{
    [Fact]
    public async Task Same_thumbnail_on_detail_summary_search_related_and_public_media()
    {
        var fam = Seed.Family();
        var genus = Seed.Genus(fam, 7);
        Seed.Species(fam, 100, "Oryzias latipes", genus);
        Seed.Species(fam, 101, "Oryzias sinensis", genus);
        // Không có PicPreferred → fallback sang ảnh đực có cờ.
        Seed.Image(100, "other.jpg");
        var male = Seed.Image(100, "male.jpg", male: true);
        await Seed.SaveAsync();

        var profile = NewProfile(out var db);
        var detail  = await profile.GetDetailAsync(100);
        var summary = (await profile.GetSummariesAsync([100])).Single();
        var search  = (await profile.SearchAsync(new GetSpeciesSearchRequest { Query = "latipes" })).Items.Single();
        var related = (await profile.GetRelatedAsync(101)).Single();
        var media   = await new MediaService(new SystemImageRepository(db), new InMemoryStorageService()).GetPreferredImageAsync(100);

        Assert.Equal(male.ObjectKey, detail!.PreferredImageKey);
        Assert.Equal(male.ObjectKey, summary.ImageKey);
        Assert.Equal(male.ObjectKey, search.ImageKey);
        Assert.Equal(male.ObjectKey, related.ImageKey);
        Assert.Equal(male.ObjectKey, media!.ObjectKey);
    }

    [Fact]
    public async Task Priority_is_preferred_then_male_then_female_then_any()
    {
        var fam = Seed.Family();
        Seed.Species(fam, 1, "A");
        Seed.Image(1, "f.jpg", female: true);
        Seed.Image(1, "m.jpg", male: true);
        var p = Seed.Image(1, "p.jpg", preferred: true);
        Seed.Species(fam, 2, "B");
        Seed.Image(2, "f.jpg", female: true);
        var m = Seed.Image(2, "m.jpg", male: true);
        Seed.Species(fam, 3, "C");
        Seed.Image(3, "plain.jpg");
        var f = Seed.Image(3, "f.jpg", female: true);
        Seed.Species(fam, 4, "D");
        var any = Seed.Image(4, "plain.png");
        await Seed.SaveAsync();

        var keys = (await NewProfile().GetSummariesAsync([1, 2, 3, 4])).Select(s => s.ImageKey);

        Assert.Equal(new[] { p.ObjectKey, m.ObjectKey, f.ObjectKey, any.ObjectKey }, keys);
        Assert.Equal($"4/{any.Id}.png", any.ObjectKey); // key luôn là SystemImage.ObjectKey
    }

    [Fact]
    public async Task Fallback_among_unflagged_images_is_stable()
    {
        var fam = Seed.Family();
        Seed.Species(fam, 100, "Oryzias latipes");
        var a = Seed.Image(100, "a.jpg");
        var b = Seed.Image(100, "b.jpg");
        await Seed.SaveAsync();
        var expected = a.Id.CompareTo(b.Id) < 0 ? a.ObjectKey : b.ObjectKey;

        var profile = NewProfile();
        var first  = (await profile.GetSummariesAsync([100])).Single().ImageKey;
        var second = (await profile.GetDetailAsync(100))!.PreferredImageKey;

        Assert.Equal(expected, first);
        Assert.Equal(expected, second);
    }

    [Fact]
    public async Task Public_media_returns_null_for_species_without_images()
    {
        var fam = Seed.Family();
        Seed.Species(fam, 100, "Oryzias latipes");
        await Seed.SaveAsync();

        await using var db = Pg.CreateDb();
        var media = await new MediaService(new SystemImageRepository(db), new InMemoryStorageService()).GetPreferredImageAsync(100);

        Assert.Null(media);
    }
}
