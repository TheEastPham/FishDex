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
/// Khoá hành vi bước confirm upload ảnh bể: file thật trên storage quyết định ảnh được giữ hay bỏ.
/// Repo thật + EF InMemory (luồng này chỉ dùng LINQ thường), storage in-memory.
/// </summary>
public sealed class AquariumMediaServiceTests : IDisposable
{
    private static readonly Guid Owner = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Stranger = Guid.Parse("99999999-9999-9999-9999-999999999999");

    // Trần ảnh bể: 1.5 MB (đúng bằng trần vẫn hợp lệ).
    private const long MaxBytes = 3L * 1024 * 1024 / 2;

    private readonly AquaHomeDbContext _db = new(
        new DbContextOptionsBuilder<AquaHomeDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private readonly InMemoryStorageService _storage = new();
    private readonly AquaHomeObjectKeys _keys = new("test");

    public void Dispose()
    {
        _db.Dispose();
    }

    private AquariumMediaService Service(Guid userId)
    {
        return new AquariumMediaService(
            new AquariumMediaRepository(_db),
            new AquariumRepository(_db),
            _storage,
            new UploadVerifier(_storage),
            new FakeUser(userId),
            _keys,
            NullLogger<AquariumMediaService>.Instance);
    }

    /// <summary>Bể của Owner + một dòng media đã xin URL (chưa chắc đã upload).</summary>
    private async Task<(Guid AquariumId, AquariumMedia Media)> SeedAsync()
    {
        var tank = new Aquarium { Id = Guid.NewGuid(), UserId = Owner, Name = "Bể medaka", CreatedAt = DateTime.UtcNow };
        var media = new AquariumMedia { AquariumId = tank.Id, FileName = "a.jpg", ContentType = "image/jpeg" };
        _db.Aquariums.Add(tank);
        _db.AquariumMedia.Add(media);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear(); // mô phỏng request mới
        return (tank.Id, media);
    }

    private Task PutAsync(AquariumMedia media, long bytes)
    {
        return _storage.PutObjectAsync(_keys.AquariumMedia(media), new byte[bytes], media.ContentType);
    }

    private async Task<bool> RowExistsAsync(Guid mediaId)
    {
        _db.ChangeTracker.Clear();
        return await _db.AquariumMedia.AnyAsync(m => m.Id == mediaId);
    }

    [Fact]
    public async Task Confirm_without_a_file_drops_the_row()
    {
        var (aquariumId, media) = await SeedAsync();

        var result = await Service(Owner).ConfirmUploadAsync(aquariumId, media.Id);

        Assert.Null(result);
        Assert.False(await RowExistsAsync(media.Id));
    }

    [Fact]
    public async Task Confirm_over_the_limit_deletes_the_object_and_drops_the_row()
    {
        var (aquariumId, media) = await SeedAsync();
        await PutAsync(media, MaxBytes + 1);

        var result = await Service(Owner).ConfirmUploadAsync(aquariumId, media.Id);

        Assert.Null(result);
        Assert.Empty(_storage.Objects);
        Assert.False(await RowExistsAsync(media.Id));
    }

    [Fact]
    public async Task Confirm_exactly_at_the_limit_is_accepted()
    {
        var (aquariumId, media) = await SeedAsync();
        await PutAsync(media, MaxBytes);

        var result = await Service(Owner).ConfirmUploadAsync(aquariumId, media.Id);

        Assert.NotNull(result);
        Assert.Equal(media.Id, result.Id);
        Assert.Equal(InMemoryStorageService.UrlFor(_keys.AquariumMedia(media)), result.Url);
        Assert.Single(_storage.Objects);
        Assert.True(await RowExistsAsync(media.Id));
    }

    [Fact]
    public async Task Confirm_by_a_stranger_changes_nothing()
    {
        var (aquariumId, media) = await SeedAsync();
        await PutAsync(media, MaxBytes + 1);

        var result = await Service(Stranger).ConfirmUploadAsync(aquariumId, media.Id);

        Assert.Null(result);
        Assert.Single(_storage.Objects); // không phải chủ bể thì không đụng tới file
        Assert.True(await RowExistsAsync(media.Id));
    }
}
