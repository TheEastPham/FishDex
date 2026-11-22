using FishDex.Domain.Storage;
using FishDex.EFCore.DbContexts;
using FishDex.EFCore.Repository;
using FishDex.Tests.Infrastructure;
using FishLover.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace FishDex.Tests.Community;

/// <summary>Job dọn <c>community/</c>: xoá object không loài nào dùng (sau thời gian chờ) và ảnh bìa bị PUT đè quá cỡ.</summary>
[Collection(PostgresCollection.Name)]
public sealed class CommunityImageJanitorTests(PostgresFixture pg) : IAsyncLifetime
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 21, 8, 0, 0, TimeSpan.Zero);

    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = T0;

        public override DateTimeOffset GetUtcNow()
        {
            return Now;
        }
    }

    private readonly Clock _clock = new();
    private InMemoryStorageService _storage = null!;
    private FishDexDbContext _db = null!;
    private Seed _seed = null!;

    public async Task InitializeAsync()
    {
        await pg.ResetAsync();
        _storage = new InMemoryStorageService(time: _clock);
        _db = pg.CreateDb();
        _seed = new Seed(_db);
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
    }

    private Task<CommunityImageCleanupResult> RunAsync()
    {
        var db = pg.CreateDb();
        var janitor = new CommunityImageJanitor(new CommunitySpeciesRepository(db), _storage, _clock, NullLogger<CommunityImageJanitor>.Instance);
        return janitor.RunAsync();
    }

    private Task PutAsync(string key, long bytes)
    {
        return _storage.PutObjectAsync(key, new byte[bytes], "image/jpeg");
    }

    private async Task<string?> ThumbnailOfAsync(int specCode)
    {
        await using var db = pg.CreateDb();
        return (await new CommunitySpeciesRepository(db).GetCommunityByCodeAsync(specCode))?.ThumbnailObjectKey;
    }

    [Fact]
    public async Task Old_orphans_are_deleted_but_covers_in_use_are_kept()
    {
        var cover = FishDexObjectKeys.CommunityCover(500001, ".jpg");
        var orphan = FishDexObjectKeys.CommunityCover(500001, ".png"); // upload rồi bỏ, không confirm
        var deletedSpecies = FishDexObjectKeys.CommunityCover(500099, ".jpg");
        _seed.Community(500001, "Hybrid", thumbnailKey: cover);
        await _seed.SaveAsync();
        await PutAsync(cover, 1000);
        await PutAsync(orphan, 5000);
        await PutAsync(deletedSpecies, 5000);

        _clock.Now = T0 + CommunityImageJanitor.OrphanGracePeriod + TimeSpan.FromMinutes(1);
        var result = await RunAsync();

        Assert.Equal(new[] { cover }, _storage.Objects.Keys);
        Assert.Equal(new CommunityImageCleanupResult(3, 2, 0), result);
    }

    [Fact]
    public async Task Recent_unconfirmed_uploads_are_left_alone()
    {
        var pending = FishDexObjectKeys.CommunityCover(500001, ".jpg");
        _seed.Community(500001, "Hybrid");
        await _seed.SaveAsync();
        await PutAsync(pending, 1000);

        _clock.Now = T0 + TimeSpan.FromMinutes(10); // người dùng có thể đang upload/confirm
        var result = await RunAsync();

        Assert.True(_storage.Objects.ContainsKey(pending));
        Assert.Equal(0, result.DeletedOrphans);
    }

    [Fact]
    public async Task Cover_overwritten_with_an_oversized_file_is_deleted_and_detached()
    {
        var cover = FishDexObjectKeys.CommunityCover(500001, ".jpg");
        _seed.Community(500001, "Hybrid", thumbnailKey: cover);
        await _seed.SaveAsync();
        await PutAsync(cover, FishDexObjectKeys.CommunityCoverMaxBytes + 1);

        var result = await RunAsync(); // không cần chờ — file quá cỡ đang được dùng thì xử lý ngay

        Assert.Empty(_storage.Objects);
        Assert.Null(await ThumbnailOfAsync(500001));
        Assert.Equal(1, result.DetachedOversized);
    }

    [Fact]
    public async Task Objects_outside_the_community_area_are_never_touched()
    {
        await PutAsync("100/3f2b1c.jpg", FishDexObjectKeys.CommunityCoverMaxBytes * 3);
        await PutAsync("communityX/old.jpg", 10);

        _clock.Now = T0 + TimeSpan.FromDays(30);
        var result = await RunAsync();

        Assert.Equal(2, _storage.Objects.Count);
        Assert.Equal(0, result.Scanned);
    }

    [Fact]
    public async Task Unconfigured_storage_is_a_no_op()
    {
        var janitor = new CommunityImageJanitor(new CommunitySpeciesRepository(_db), new InMemoryStorageService(configured: false), _clock, NullLogger<CommunityImageJanitor>.Instance);

        Assert.Equal(new CommunityImageCleanupResult(0, 0, 0), await janitor.RunAsync());
    }
}
