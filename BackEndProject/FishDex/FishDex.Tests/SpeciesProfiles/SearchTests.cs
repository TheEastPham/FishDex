using FishDex.Domain.DTOs.Species;
using FishDex.Tests.Infrastructure;

namespace FishDex.Tests.SpeciesProfiles;

[Collection(PostgresCollection.Name)]
public sealed class SearchTests(PostgresFixture pg) : SpeciesProfileTestBase(pg)
{
    private async Task SeedTwoCommunityAndThreeFishBaseAsync()
    {
        var fam = Seed.Family();
        Seed.Community(500001, "Oryzias hybrid A");
        Seed.Community(500002, "Oryzias hybrid B");
        Seed.Species(fam, 100, "Oryzias latipes");
        Seed.Species(fam, 101, "Oryzias minutillus");
        Seed.Species(fam, 102, "Oryzias sinensis");
        await Seed.SaveAsync();
    }

    [Fact]
    public async Task Pages_community_first_then_fishbase()
    {
        await SeedTwoCommunityAndThreeFishBaseAsync();

        var profile = NewProfile();
        var page1 = await profile.SearchAsync(new GetSpeciesSearchRequest { Query = "oryzias", PageSize = 2, Page = 1 });
        var page2 = await profile.SearchAsync(new GetSpeciesSearchRequest { Query = "oryzias", PageSize = 2, Page = 2 });
        var page3 = await profile.SearchAsync(new GetSpeciesSearchRequest { Query = "oryzias", PageSize = 2, Page = 3 });

        Assert.Equal(5, page1.TotalCount);
        Assert.Equal(new[] { 500001, 500002 }, page1.Items.Select(i => i.SpecCode));
        Assert.Equal(new[] { 100, 101 }, page2.Items.Select(i => i.SpecCode));
        Assert.Equal(new[] { 102 }, page3.Items.Select(i => i.SpecCode));
    }

    [Fact]
    public async Task A_page_can_straddle_both_sources()
    {
        await SeedTwoCommunityAndThreeFishBaseAsync();

        var profile = NewProfile();
        var page1 = await profile.SearchAsync(new GetSpeciesSearchRequest { Query = "oryzias", PageSize = 3, Page = 1 });
        var page2 = await profile.SearchAsync(new GetSpeciesSearchRequest { Query = "oryzias", PageSize = 3, Page = 2 });

        Assert.Equal(new[] { 500001, 500002, 100 }, page1.Items.Select(i => i.SpecCode));
        Assert.Equal(new[] { 101, 102 }, page2.Items.Select(i => i.SpecCode));
    }

    [Fact]
    public async Task Page_beyond_the_end_is_empty_but_keeps_the_total()
    {
        await SeedTwoCommunityAndThreeFishBaseAsync();

        var page = await NewProfile().SearchAsync(new GetSpeciesSearchRequest { Query = "oryzias", PageSize = 20, Page = 5 });

        Assert.Empty(page.Items);
        Assert.Equal(5, page.TotalCount);
    }

    [Fact]
    public async Task Taxonomy_filter_excludes_community_species()
    {
        var fam = Seed.Family();
        var genus = Seed.Genus(fam, 7);
        Seed.Community(500001, "Oryzias hybrid A");
        Seed.Species(fam, 100, "Oryzias latipes", genus);
        Seed.Species(fam, 101, "Oryzias sinensis");
        await Seed.SaveAsync();

        var profile = NewProfile();
        var byFamily = await profile.SearchAsync(new GetSpeciesSearchRequest { FamId = fam.Id });
        var byGenus  = await profile.SearchAsync(new GetSpeciesSearchRequest { GenusCode = 7 });

        Assert.Equal(new[] { 100, 101 }, byFamily.Items.Select(i => i.SpecCode));
        Assert.Equal(new[] { 100 }, byGenus.Items.Select(i => i.SpecCode));
    }

    [Fact]
    public async Task Unverified_community_species_are_not_searchable()
    {
        Seed.Community(500001, "Oryzias hybrid A", verified: false);
        await Seed.SaveAsync();

        var page = await NewProfile().SearchAsync(new GetSpeciesSearchRequest { Query = "oryzias" });

        Assert.Empty(page.Items);
        Assert.Equal(0, page.TotalCount);
    }

    [Fact]
    public async Task Matches_verified_common_names_in_the_requested_language_only()
    {
        var fam = Seed.Family();
        Seed.Species(fam, 100, "Oryzias latipes");
        Seed.Name(100, "Cá medaka", "Vietnamese");
        Seed.Name(100, "Medaka", "English", isPreferred: true);
        Seed.Species(fam, 200, "Danio rerio");
        Seed.Name(200, "Cá ngựa vằn chưa duyệt", "Vietnamese", isVerified: false, contributedBy: Guid.NewGuid());
        await Seed.SaveAsync();

        var profile = NewProfile();
        var vi = await profile.SearchAsync(new GetSpeciesSearchRequest { Query = "cá", Language = "vi" });
        var en = await profile.SearchAsync(new GetSpeciesSearchRequest { Query = "cá medaka", Language = "en" });

        var hit = Assert.Single(vi.Items);
        Assert.Equal(100, hit.SpecCode);
        Assert.Equal("Cá medaka", hit.PreferredCommonName);
        Assert.Empty(en.Items);
    }

    [Fact]
    public async Task Card_carries_taxonomy_name_and_thumbnail()
    {
        var fam = Seed.Family(name: "Adrianichthyidae");
        var genus = Seed.Genus(fam, 7, "Oryzias");
        Seed.Species(fam, 100, "Oryzias latipes", genus);
        Seed.Name(100, "Japanese rice fish", "English", isPreferred: true);
        var img = Seed.Image(100, "only.jpg");
        await Seed.SaveAsync();

        var card = Assert.Single((await NewProfile().SearchAsync(new GetSpeciesSearchRequest())).Items);

        Assert.Equal("Oryzias", card.GenusName);
        Assert.Equal("Adrianichthyidae", card.FamilyName);
        Assert.Equal("Japanese rice fish", card.PreferredCommonName);
        Assert.Equal(img.ObjectKey, card.ImageKey);
        Assert.Equal(FakeStorage.Url(img.ObjectKey), card.ImageUrl);
    }
}
