using FishDex.Tests.Infrastructure;

namespace FishDex.Tests.SpeciesProfiles;

[Collection(PostgresCollection.Name)]
public sealed class RelatedTests(PostgresFixture pg) : SpeciesProfileTestBase(pg)
{
    [Fact]
    public async Task Prefers_same_genus_then_backfills_with_same_family()
    {
        var fam = Seed.Family();
        var g1 = Seed.Genus(fam, 7, "Oryzias");
        var g2 = Seed.Genus(fam, 8, "Adrianichthys");
        Seed.Species(fam, 100, "Oryzias latipes", g1);
        Seed.Species(fam, 101, "Oryzias sinensis", g1);
        Seed.Species(fam, 102, "Adrianichthys kruyti", g2);
        var other = Seed.Family(famCode: 2, name: "Cyprinidae");
        Seed.Species(other, 200, "Danio rerio");
        await Seed.SaveAsync();

        var related = await NewProfile().GetRelatedAsync(100, limit: 3);

        Assert.Equal(new[] { 101, 102 }, related.Select(r => r.SpecCode));
    }

    [Fact]
    public async Task Species_without_genus_uses_family_only()
    {
        var fam = Seed.Family();
        Seed.Species(fam, 100, "Oryzias sp.");
        Seed.Species(fam, 101, "Oryzias latipes");
        await Seed.SaveAsync();

        var related = await NewProfile().GetRelatedAsync(100);

        Assert.Equal(new[] { 101 }, related.Select(r => r.SpecCode));
    }

    [Fact]
    public async Task Limit_is_clamped_between_three_and_six()
    {
        var fam = Seed.Family();
        var genus = Seed.Genus(fam, 7);
        Seed.Species(fam, 100, "Oryzias anchor", genus);
        for (var i = 1; i <= 8; i++)
            Seed.Species(fam, 100 + i, $"Oryzias sp{i}", genus);
        await Seed.SaveAsync();

        var profile = NewProfile();

        Assert.Equal(6, (await profile.GetRelatedAsync(100, limit: 50)).Count);
        Assert.Equal(3, (await profile.GetRelatedAsync(100, limit: 1)).Count);
    }

    [Fact]
    public async Task Unknown_or_community_code_has_no_related_species()
    {
        Seed.Community(500001, "Oryzias hybrid");
        await Seed.SaveAsync();

        var profile = NewProfile();

        Assert.Empty(await profile.GetRelatedAsync(123));
        Assert.Empty(await profile.GetRelatedAsync(500001));
    }
}
