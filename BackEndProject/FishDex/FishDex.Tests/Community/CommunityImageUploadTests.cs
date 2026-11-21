using FishDex.Domain.DTOs.Species;
using FishDex.Domain.Services;
using FishDex.Domain.Services.Interfaces;
using FishDex.Domain.Storage;
using FishDex.EFCore.DbContexts;
using FishDex.EFCore.Repository;
using FishDex.EFCore.Repository.Interface;
using FishDex.Tests.Infrastructure;
using FishLover.Shared.Common.Enum;
using FishLover.Storage;
using FishLover.Storage.MediaAccess;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FishDex.Tests.Community;

/// <summary>
/// Upload ảnh loài cộng đồng: presigned PUT không chặn được dung lượng, nên ba lớp BE phải chặn —
/// confirm (HEAD), trần số loài chưa duyệt, và redeem media-access (HEAD lại).
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class CommunityImageUploadTests(PostgresFixture pg) : IAsyncLifetime
{
    private const long OneMb = FishDexObjectKeys.CommunityCoverMaxBytes;

    private readonly InMemoryStorageService _storage = new();
    private readonly FakeUser _user = new(Guid.NewGuid());
    private FishDexDbContext _db = null!;
    private CommunitySpeciesService _service = null!;

    public async Task InitializeAsync()
    {
        await pg.ResetAsync();
        _db = pg.CreateDb();
        _service = new CommunitySpeciesService(new CommunitySpeciesRepository(_db), _storage, _user, NullLogger<CommunitySpeciesService>.Instance);
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
    }

    private async Task<int> SubmitAsync(string name = "Oryzias hybrid")
    {
        var result = await _service.SubmitAsync(new SubmitCommunitySpeciesRequest(name, WaterType.Freshwater));
        Assert.Equal(SubmitCommunitySpeciesOutcome.Created, result.Outcome);
        return result.Dto!.SpecCode;
    }

    private async Task<string?> ThumbnailOfAsync(int specCode)
    {
        await using var db = pg.CreateDb();
        return (await new CommunitySpeciesRepository(db).GetCommunityByCodeAsync(specCode))?.ThumbnailObjectKey;
    }

    private Task PutAsync(string key, long bytes)
    {
        return _storage.PutObjectAsync(key, new byte[bytes], "image/jpeg");
    }

    // ── Confirm ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Presign_alone_does_not_attach_the_image()
    {
        var specCode = await SubmitAsync();

        var upload = await _service.RequestImageUploadAsync(specCode, "a.jpg", "image/jpeg");

        Assert.Equal(FishDexObjectKeys.CommunityCover(specCode, ".jpg"), upload!.ObjectKey);
        Assert.Null(await ThumbnailOfAsync(specCode));
    }

    [Fact]
    public async Task Confirm_attaches_an_image_within_the_limit()
    {
        var specCode = await SubmitAsync();
        var upload = await _service.RequestImageUploadAsync(specCode, "a.jpg", "image/jpeg");
        await PutAsync(upload!.ObjectKey, OneMb);

        Assert.Equal(ConfirmCommunityImageOutcome.Confirmed, await _service.ConfirmImageUploadAsync(specCode, upload.ObjectKey));
        Assert.Equal(upload.ObjectKey, await ThumbnailOfAsync(specCode));
    }

    [Fact]
    public async Task Confirm_rejects_and_deletes_an_oversized_upload()
    {
        var specCode = await SubmitAsync();
        var upload = await _service.RequestImageUploadAsync(specCode, "a.jpg", "image/jpeg");
        await PutAsync(upload!.ObjectKey, OneMb + 1);

        Assert.Equal(ConfirmCommunityImageOutcome.TooLarge, await _service.ConfirmImageUploadAsync(specCode, upload.ObjectKey));
        Assert.False(_storage.Objects.ContainsKey(upload.ObjectKey));
        Assert.Null(await ThumbnailOfAsync(specCode));
    }

    [Fact]
    public async Task Oversized_upload_over_the_current_cover_detaches_it()
    {
        var specCode = await SubmitAsync();
        var first = await _service.RequestImageUploadAsync(specCode, "a.jpg", "image/jpeg");
        await PutAsync(first!.ObjectKey, 1000);
        await _service.ConfirmImageUploadAsync(specCode, first.ObjectKey);

        // Cùng định dạng = cùng key: PUT đè ảnh bìa đang dùng bằng file quá cỡ.
        await _service.RequestImageUploadAsync(specCode, "b.jpg", "image/jpeg");
        await PutAsync(first.ObjectKey, OneMb * 3);

        Assert.Equal(ConfirmCommunityImageOutcome.TooLarge, await _service.ConfirmImageUploadAsync(specCode, first.ObjectKey));
        Assert.Null(await ThumbnailOfAsync(specCode));
    }

    [Fact]
    public async Task Confirm_without_upload_reports_not_uploaded()
    {
        var specCode = await SubmitAsync();
        var upload = await _service.RequestImageUploadAsync(specCode, "a.jpg", "image/jpeg");

        Assert.Equal(ConfirmCommunityImageOutcome.NotUploaded, await _service.ConfirmImageUploadAsync(specCode, upload!.ObjectKey));
    }

    [Fact]
    public async Task Confirm_only_accepts_this_species_cover_key_from_its_owner()
    {
        var mine = await SubmitAsync("Mine");
        var other = await SubmitAsync("Other");
        await PutAsync("100/abc.jpg", 10);
        await PutAsync(FishDexObjectKeys.CommunityCover(other, ".jpg"), 10);

        Assert.Equal(ConfirmCommunityImageOutcome.NotFound, await _service.ConfirmImageUploadAsync(mine, "100/abc.jpg"));
        Assert.Equal(ConfirmCommunityImageOutcome.NotFound, await _service.ConfirmImageUploadAsync(mine, FishDexObjectKeys.CommunityCover(other, ".jpg")));
        Assert.Equal(ConfirmCommunityImageOutcome.NotFound, await _service.ConfirmImageUploadAsync(mine, FishDexObjectKeys.CommunityCover(mine, ".gif")));

        _user.UserId = Guid.NewGuid();
        Assert.Equal(ConfirmCommunityImageOutcome.NotFound, await _service.ConfirmImageUploadAsync(other, FishDexObjectKeys.CommunityCover(other, ".jpg")));
    }

    [Fact]
    public async Task Changing_format_replaces_the_old_cover_only_after_confirm()
    {
        var specCode = await SubmitAsync();
        var jpg = await _service.RequestImageUploadAsync(specCode, "a.jpg", "image/jpeg");
        await PutAsync(jpg!.ObjectKey, 1000);
        await _service.ConfirmImageUploadAsync(specCode, jpg.ObjectKey);

        var png = await _service.RequestImageUploadAsync(specCode, "b.png", "image/png");
        Assert.True(_storage.Objects.ContainsKey(jpg.ObjectKey)); // ảnh đang dùng còn nguyên tới khi ảnh mới qua kiểm tra

        await PutAsync(png!.ObjectKey, 1000);
        await _service.ConfirmImageUploadAsync(specCode, png.ObjectKey);

        Assert.Equal(png.ObjectKey, await ThumbnailOfAsync(specCode));
        Assert.False(_storage.Objects.ContainsKey(jpg.ObjectKey));
    }

    [Fact]
    public async Task Presign_cleans_up_unconfirmed_uploads_in_other_formats()
    {
        var specCode = await SubmitAsync();
        var png = await _service.RequestImageUploadAsync(specCode, "a.png", "image/png");
        await PutAsync(png!.ObjectKey, OneMb * 5); // upload xong nhưng không bao giờ confirm

        await _service.RequestImageUploadAsync(specCode, "b.webp", "image/webp");

        Assert.False(_storage.Objects.ContainsKey(png.ObjectKey));
    }

    [Fact]
    public async Task Deleting_a_species_removes_every_object_in_its_folder()
    {
        var specCode = await SubmitAsync();
        var other = await SubmitAsync("Other");
        await PutAsync(FishDexObjectKeys.CommunityCover(specCode, ".jpg"), 10);
        await PutAsync(FishDexObjectKeys.CommunityCover(specCode, ".png"), 10);
        await PutAsync(FishDexObjectKeys.CommunityCover(other, ".jpg"), 10);

        Assert.True(await _service.DeleteAsync(specCode));

        Assert.Equal(new[] { FishDexObjectKeys.CommunityCover(other, ".jpg") }, _storage.Objects.Keys);
    }

    // ── Trần số loài chưa duyệt ─────────────────────────────────────────────

    [Fact]
    public async Task Sixth_unverified_species_is_refused_until_one_is_verified()
    {
        var codes = new List<int>();
        for (var i = 0; i < 5; i++)
        {
            codes.Add(await SubmitAsync($"Hybrid {i}"));
        }

        var refused = await _service.SubmitAsync(new SubmitCommunitySpeciesRequest("Hybrid 6", WaterType.Freshwater));
        Assert.Equal(SubmitCommunitySpeciesOutcome.PendingLimitReached, refused.Outcome);

        await _service.VerifyAsync(codes[0], null);
        await SubmitAsync("Hybrid 6");
    }

    [Fact]
    public async Task Rejected_species_still_count_toward_the_limit()
    {
        for (var i = 0; i < 5; i++)
        {
            await _service.RejectAsync(await SubmitAsync($"Hybrid {i}"), "trùng loài");
        }

        var refused = await _service.SubmitAsync(new SubmitCommunitySpeciesRequest("Hybrid 6", WaterType.Freshwater));
        Assert.Equal(SubmitCommunitySpeciesOutcome.PendingLimitReached, refused.Outcome);
    }

    [Fact]
    public async Task The_limit_is_per_user()
    {
        for (var i = 0; i < 5; i++)
        {
            await SubmitAsync($"Hybrid {i}");
        }

        _user.UserId = Guid.NewGuid();
        await SubmitAsync("Someone else");
    }

    // ── Redeem media-access: HEAD lại lúc phục vụ ảnh ───────────────────────

    [Fact]
    public async Task Media_access_refuses_a_cover_overwritten_after_confirm()
    {
        var specCode = await SubmitAsync();
        var upload = await _service.RequestImageUploadAsync(specCode, "a.jpg", "image/jpeg");
        await PutAsync(upload!.ObjectKey, 1000);
        await _service.ConfirmImageUploadAsync(specCode, upload.ObjectKey);
        await _service.VerifyAsync(specCode, null);

        var mediaAccess = NewMediaAccess();
        var sas = mediaAccess.CreateSharedSignature(600)!.Sas.Split('&').Select(p => p.Split('=', 2)).ToDictionary(p => p[0], p => p[1]);

        var ok = await mediaAccess.RedeemAsync(upload.ObjectKey, sas["shared"], sas["exp"], sas["sig"]);
        Assert.Null(ok.Denial);

        // Dùng lại presigned URL còn hạn để PUT đè file lớn — confirm không chạy lại.
        await PutAsync(upload.ObjectKey, OneMb * 50);

        var refused = await mediaAccess.RedeemAsync(upload.ObjectKey, sas["shared"], sas["exp"], sas["sig"]);
        Assert.Equal(MediaAccessDenial.Invalid, refused.Denial);
    }

    private MediaAccessService NewMediaAccess()
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => pg.CreateDb());
        services.AddScoped<ICommunitySpeciesRepository, CommunitySpeciesRepository>();
        var provider = services.BuildServiceProvider();

        var settings = Options.Create(new MediaAccessSettings { Key = Convert.ToBase64String(new byte[32]) });
        var signer = new MediaAccessSigner(settings, TimeProvider.System, NullLogger<MediaAccessSigner>.Instance);
        return new MediaAccessService(signer, _storage, provider.GetRequiredService<IServiceScopeFactory>());
    }
}
