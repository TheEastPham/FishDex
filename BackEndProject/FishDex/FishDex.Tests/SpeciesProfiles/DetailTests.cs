using FishDex.Tests.Infrastructure;
using FishLover.Storage;

namespace FishDex.Tests.SpeciesProfiles;

[Collection(PostgresCollection.Name)]
public sealed class DetailTests(PostgresFixture pg) : SpeciesProfileTestBase(pg)
{
    // ── Ecology ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Uses_the_real_ecology_row_not_the_junk_row_from_old_etl()
    {
        var fam = Seed.Family();
        Seed.Species(fam, 100, "Oryzias latipes");
        Seed.Ecology(100, ecologyId: 1, autoctr: 999); // rác: EcologyId ≠ autoctr, không có sub-row
        Seed.Ecology(100, ecologyId: 2, autoctr: 2);   // thật
        Seed.Feeding(2, "omnivore", dietTroph: 0m, foodTroph: 3.1m);
        Seed.Associations(2, schooling: true);
        await Seed.SaveAsync();

        var detail = await NewProfile().GetDetailAsync(100);

        Assert.NotNull(detail?.Ecology);
        Assert.Equal("omnivore", detail!.Ecology!.FeedingType);
        Assert.True(detail.Ecology.Schooling);
    }

    [Fact]
    public async Task Trophic_level_prefers_DietTroph_and_treats_zero_as_missing()
    {
        var fam = Seed.Family();
        Seed.Species(fam, 100, "Oryzias latipes");
        Seed.Ecology(100, 1, 1);
        Seed.Feeding(1, "omnivore", dietTroph: 2.8m, foodTroph: 3.1m);
        Seed.Species(fam, 200, "Oryzias sinensis");
        Seed.Ecology(200, 2, 2);
        Seed.Feeding(2, "omnivore", dietTroph: 0m, foodTroph: 3.1m); // ETL cũ ghi 0 thay vì NULL
        Seed.Species(fam, 300, "Oryzias celebensis");
        Seed.Ecology(300, 3, 3);
        Seed.Feeding(3, "omnivore", dietTroph: 0m, foodTroph: 0m);
        await Seed.SaveAsync();

        var profile = NewProfile();

        Assert.Equal(2.8m, (await profile.GetDetailAsync(100))!.Ecology!.DietTroph);
        Assert.Equal(3.1m, (await profile.GetDetailAsync(200))!.Ecology!.DietTroph);
        Assert.Null((await profile.GetDetailAsync(300))!.Ecology!.DietTroph);
    }

    [Fact]
    public async Task Ecology_block_is_null_when_the_chosen_row_has_no_sub_rows()
    {
        var fam = Seed.Family();
        Seed.Species(fam, 100, "Oryzias latipes");
        Seed.Ecology(100, 1, 1);
        await Seed.SaveAsync();

        var detail = await NewProfile().GetDetailAsync(100);

        Assert.Null(detail!.Ecology);
        Assert.Null(detail.Habitat);
    }

    [Fact]
    public async Task Maps_habitat_zones_substrates_and_special_habitats()
    {
        var fam = Seed.Family();
        Seed.Species(fam, 100, "Oryzias latipes");
        Seed.Ecology(100, 1, 1);
        Seed.HabitatZone(1, stream: true, lakes: true);
        Seed.Substrate(1, sand: true, gravel: true, endofauna: true);
        Seed.SpecialHabitat(1, driftwood: true, macrophyte: true);
        await Seed.SaveAsync();

        var detail = await NewProfile().GetDetailAsync(100);

        Assert.Equal(new[] { "Stream", "Lakes" }, detail!.Ecology!.HabitatZones);
        Assert.Equal(new[] { "Sand", "Gravel" }, detail.Habitat!.PreferredSubstrates);
        Assert.True(detail.Habitat.BurrowingCapable);
        Assert.True(detail.Habitat.RequiresDriftwood);
        Assert.True(detail.Habitat.RequiresVegetation);
        Assert.False(detail.Habitat.RequiresCaves);
    }

    // ── Stock ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Reads_environment_and_conservation_from_the_lowest_stock()
    {
        var fam = Seed.Family();
        Seed.Species(fam, 100, "Oryzias latipes");
        Seed.Stock(100, stockCode: 20, tempMin: 30);
        Seed.Stock(100, stockCode: 10, tempMin: 18, iucn: "LC");
        await Seed.SaveAsync();

        var detail = await NewProfile().GetDetailAsync(100);

        Assert.Equal(18, detail!.Environment!.TempMin);
        Assert.Equal("LC", detail.Conservation!.IucnCode);
    }

    [Fact]
    public async Task Species_without_stock_or_ecology_still_returns_the_basics()
    {
        var fam = Seed.Family(name: "Adrianichthyidae");
        var genus = Seed.Genus(fam, 7, "Oryzias");
        Seed.Species(fam, 100, "Oryzias latipes", genus);
        await Seed.SaveAsync();

        var detail = await NewProfile().GetDetailAsync(100);

        Assert.Equal("Oryzias latipes", detail!.SpeciesName);
        Assert.Equal("Oryzias", detail.GenusName);
        Assert.Equal("Adrianichthyidae", detail.FamilyName);
        Assert.Equal("Beloniformes", detail.OrderName);
        Assert.Equal("Freshwater", detail.WaterType);
        Assert.Null(detail.Environment);
        Assert.Null(detail.Conservation);
        Assert.Null(detail.Ecology);
    }

    // ── Ảnh ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Male_and_female_images_use_flag_first_then_name_match()
    {
        var fam = Seed.Family();
        Seed.Species(fam, 100, "Oryzias latipes", picNameMale: "byname_m.jpg", picNameFemale: "byname_f.jpg");
        Seed.Image(100, "byname_m.jpg");
        var maleFlag = Seed.Image(100, "flag_m.jpg", male: true);
        var femaleByName = Seed.Image(100, "byname_f.jpg");
        await Seed.SaveAsync();

        var detail = await NewProfile().GetDetailAsync(100);

        Assert.Equal(maleFlag.ObjectKey, detail!.MaleImageKey);      // có cờ → cờ thắng tên
        Assert.Equal(femaleByName.ObjectKey, detail.FemaleImageKey); // không ảnh nào có cờ cái → khớp tên
    }

    [Fact]
    public async Task Male_and_female_images_are_null_when_nothing_matches_never_guessed()
    {
        var fam = Seed.Family();
        Seed.Species(fam, 100, "Oryzias latipes", picNameMale: "missing.jpg");
        Seed.Image(100, "plain.jpg");
        await Seed.SaveAsync();

        var detail = await NewProfile().GetDetailAsync(100);

        Assert.NotNull(detail!.PreferredImageKey); // thumbnail vẫn fallback được
        Assert.Null(detail.MaleImageKey);
        Assert.Null(detail.FemaleImageKey);
        Assert.Null(detail.MaleImageUrl);
    }

    [Fact]
    public async Task Image_keys_are_returned_even_when_storage_is_not_configured()
    {
        var fam = Seed.Family();
        Seed.Species(fam, 100, "Oryzias latipes");
        var img = Seed.Image(100, "best.jpg", preferred: true);
        await Seed.SaveAsync();

        var detail = await NewProfile(storageConfigured: false).GetDetailAsync(100);

        Assert.Equal(img.ObjectKey, detail!.PreferredImageKey);
        Assert.Null(detail.PreferredImageUrl);
    }

    // ── Tên & nguồn ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Common_name_is_null_when_neither_requested_language_nor_english_exists()
    {
        var fam = Seed.Family();
        Seed.Species(fam, 100, "Oryzias latipes");
        Seed.Name(100, "メダカ", "Japanese", isPreferred: true);
        await Seed.SaveAsync();

        var detail = await NewProfile().GetDetailAsync(100, "vi");

        Assert.Null(detail!.PreferredCommonName);
    }

    [Fact]
    public async Task Unknown_fishbase_code_returns_null()
    {
        Assert.Null(await NewProfile().GetDetailAsync(123));
    }

    [Fact]
    public async Task Community_detail_requires_verification()
    {
        Seed.Community(500001, "Verified hybrid", verified: true);
        Seed.Community(500002, "Pending hybrid", verified: false);
        await Seed.SaveAsync();

        var profile = NewProfile();

        Assert.Equal("Verified hybrid", (await profile.GetDetailAsync(500001))!.SpeciesName);
        Assert.Null(await profile.GetDetailAsync(500002));
    }

    /// <summary>
    /// Tên của loài cộng đồng là tên người gửi nhập lúc submit, ở mọi ngôn ngữ.
    /// </summary>
    [Fact]
    public async Task Community_species_use_the_name_entered_at_submission()
    {
        Seed.Community(500001, "Oryzias hybrid", commonName: "Medaka cam");
        await Seed.SaveAsync();

        var profile = NewProfile();

        Assert.Equal("Medaka cam", (await profile.GetDetailAsync(500001, "vi"))!.PreferredCommonName);
        Assert.Equal("Medaka cam", (await profile.GetDetailAsync(500001, "en"))!.PreferredCommonName);
    }

    [Fact]
    public async Task Community_detail_maps_ecology_and_environment_only_when_present()
    {
        var hybrid = Seed.Community(500001, "Oryzias hybrid", thumbnailKey: "community/500001/t.jpg");
        hybrid.TempMin = 18;
        hybrid.Schooling = true;
        Seed.Community(500002, "Bare hybrid");
        await Seed.SaveAsync();

        var profile = NewProfile();
        var rich = await profile.GetDetailAsync(500001);
        var bare = await profile.GetDetailAsync(500002);

        Assert.Equal(18, rich!.Environment!.TempMin);
        Assert.True(rich.Ecology!.Schooling);
        Assert.Equal(InMemoryStorageService.UrlFor("community/500001/t.jpg"), rich.PreferredImageUrl);
        Assert.Null(bare!.Environment);
        Assert.Null(bare.Ecology);
    }
}
