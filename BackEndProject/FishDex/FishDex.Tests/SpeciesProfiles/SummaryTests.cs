using FishDex.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FishDex.Tests.SpeciesProfiles;

/// <summary>Summaries là hợp đồng mà AquaHome và MarketService dùng.</summary>
[Collection(PostgresCollection.Name)]
public sealed class SummaryTests(PostgresFixture pg) : SpeciesProfileTestBase(pg)
{
    [Fact]
    public async Task Mixes_both_sources_keeps_input_order_and_skips_unknown_codes()
    {
        var fam = Seed.Family();
        Seed.Species(fam, 100, "Oryzias latipes");
        Seed.Community(500001, "Oryzias hybrid 'Yōkihi'", verified: false, commonName: "Medaka cam", thumbnailKey: "community/500001/t.jpg");
        await Seed.SaveAsync();

        var summaries = await NewProfile().GetSummariesAsync([500001, 424242, 100]);

        Assert.Equal(new[] { 500001, 100 }, summaries.Select(s => s.SpecCode));
        Assert.Equal("Medaka cam", summaries[0].CommonName);
        Assert.Equal("community/500001/t.jpg", summaries[0].ImageKey);
        Assert.Equal(3.5m, summaries[0].Length); // market dùng làm fallback
        Assert.Equal(4m, summaries[1].Length);
    }

    [Fact]
    public async Task Empty_and_duplicate_codes()
    {
        var fam = Seed.Family();
        Seed.Species(fam, 100, "Oryzias latipes");
        await Seed.SaveAsync();

        var profile = NewProfile();

        Assert.Empty(await profile.GetSummariesAsync([]));
        Assert.Single(await profile.GetSummariesAsync([100, 100, 100]));
    }

    /// <summary>
    /// Trước khi migration DropFishBaseSpeciesSnapshots chạy, bảng còn bản cache FishBase cũ với
    /// tên/ảnh lỗi thời. Profile phải đọc bảng FishBase gốc, bỏ qua hoàn toàn các dòng đó.
    /// </summary>
    [Fact]
    public async Task Ignores_stale_fishbase_rows_left_in_the_snapshot_table()
    {
        var fam = Seed.Family();
        Seed.Species(fam, 100, "Oryzias latipes");
        Seed.Name(100, "Japanese rice fish", "English", isPreferred: true);
        var img = Seed.Image(100, "best.jpg", preferred: true);
        Seed.StaleFishBaseSnapshot(100, "Old name", "Old common name", "100/old-key.jpg");
        await Seed.SaveAsync();

        var summary = (await NewProfile().GetSummariesAsync([100])).Single();

        Assert.Equal("Oryzias latipes", summary.SpeciesName);
        Assert.Equal("Japanese rice fish", summary.CommonName);
        Assert.Equal(img.ObjectKey, summary.ImageKey);
    }

    [Fact]
    public async Task Fishbase_species_without_images_has_no_image()
    {
        var fam = Seed.Family();
        Seed.Species(fam, 100, "Oryzias latipes");
        await Seed.SaveAsync();

        var summary = (await NewProfile().GetSummariesAsync([100])).Single();

        Assert.Null(summary.ImageKey);
        Assert.Null(summary.ImageUrl);
    }

    // ── Tên thường gọi ──────────────────────────────────────────────────────

    [Fact]
    public async Task Common_name_uses_requested_language_then_english()
    {
        var fam = Seed.Family();
        Seed.Species(fam, 100, "Oryzias latipes");
        Seed.Name(100, "Japanese rice fish", "English", isPreferred: true);
        Seed.Name(100, "Cá medaka", "Vietnamese", rank: 2);
        await Seed.SaveAsync();

        var profile = NewProfile();

        Assert.Equal("Cá medaka", (await profile.GetSummariesAsync([100], "vi")).Single().CommonName);
        Assert.Equal("Cá medaka", (await profile.GetSummariesAsync([100], "Vietnamese")).Single().CommonName);
        Assert.Equal("Japanese rice fish", (await profile.GetSummariesAsync([100])).Single().CommonName);
        Assert.Equal("Japanese rice fish", (await profile.GetSummariesAsync([100], "fr")).Single().CommonName);
    }

    [Fact]
    public async Task Within_a_language_preferred_name_wins_then_lowest_rank()
    {
        var fam = Seed.Family();
        Seed.Species(fam, 100, "Oryzias latipes");
        Seed.Name(100, "Rank one", "English", rank: 1);
        Seed.Name(100, "Preferred", "English", isPreferred: true, rank: 5);
        Seed.Species(fam, 200, "Oryzias sinensis");
        Seed.Name(200, "Rank three", "English", rank: 3);
        Seed.Name(200, "Rank two", "English", rank: 2);
        await Seed.SaveAsync();

        var summaries = await NewProfile().GetSummariesAsync([100, 200]);

        Assert.Equal("Preferred", summaries[0].CommonName);
        Assert.Equal("Rank two", summaries[1].CommonName);
    }

    [Fact]
    public async Task Contributed_name_loses_to_fishbase_name_in_the_same_language()
    {
        var fam = Seed.Family();
        Seed.Species(fam, 100, "Oryzias latipes");
        Seed.Name(100, "Cá medaka", "Vietnamese", rank: 1);
        Seed.Name(100, "Cá sóc", "Vietnamese", rank: 999, contributedBy: Guid.NewGuid());
        await Seed.SaveAsync();

        var summary = (await NewProfile().GetSummariesAsync([100], "vi")).Single();

        Assert.Equal("Cá medaka", summary.CommonName);
    }

    [Fact]
    public async Task Verified_contributed_name_shows_without_any_invalidation()
    {
        var fam = Seed.Family();
        Seed.Species(fam, 100, "Oryzias latipes");
        Seed.Name(100, "Cá sóc", "Vietnamese", rank: 999, contributedBy: Guid.NewGuid());
        await Seed.SaveAsync();

        var summary = (await NewProfile().GetSummariesAsync([100], "vi")).Single();

        Assert.Equal("Cá sóc", summary.CommonName);
    }

    [Fact]
    public async Task Unverified_contributed_name_is_not_shown()
    {
        var fam = Seed.Family();
        Seed.Species(fam, 100, "Oryzias latipes");
        Seed.Name(100, "Tên chưa duyệt", "Vietnamese", rank: 999, isVerified: false, contributedBy: Guid.NewGuid());
        await Seed.SaveAsync();

        var summary = (await NewProfile().GetSummariesAsync([100], "vi")).Single();

        Assert.Null(summary.CommonName);
    }

    // ── Không ghi trong GET ─────────────────────────────────────────────────

    [Fact]
    public async Task Read_paths_never_write_to_the_database()
    {
        var fam = Seed.Family();
        Seed.Species(fam, 100, "Oryzias latipes");
        await Seed.SaveAsync();

        var profile = NewProfile(out var db);
        await profile.GetDetailAsync(100);
        await profile.GetSummariesAsync([100, 999]);

        Assert.Equal(0, await db.SpeciesSnapshots.CountAsync());
        Assert.False(db.ChangeTracker.HasChanges());
    }
}
