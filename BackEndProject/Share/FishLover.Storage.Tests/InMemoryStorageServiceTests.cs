namespace FishLover.Storage.Tests;

/// <summary>Adapter in-memory phải giữ cùng hợp đồng với S3StorageService, để test các service khác tin được nó.</summary>
public sealed class InMemoryStorageServiceTests
{
    [Fact]
    public async Task Put_then_size_then_delete()
    {
        IStorageService storage = new InMemoryStorageService();

        await storage.PutObjectAsync("a/b.json", [1, 2, 3], "application/json");
        Assert.Equal(3, await storage.GetObjectSizeAsync("a/b.json"));

        await storage.DeleteAsync("a/b.json");
        Assert.Null(await storage.GetObjectSizeAsync("a/b.json"));
    }

    [Fact]
    public async Task Delete_by_prefix_only_removes_matching_keys()
    {
        var storage = new InMemoryStorageService();
        await storage.PutObjectAsync("aquahome/dev/articles/1/cover.jpg", [1], "image/jpeg");
        await storage.PutObjectAsync("aquahome/dev/articles/1/en/content.json", [1], "application/json");
        await storage.PutObjectAsync("aquahome/dev/articles/2/cover.jpg", [1], "image/jpeg");

        await storage.DeleteByPrefixAsync("aquahome/dev/articles/1");
        await storage.DeleteByPrefixAsync("   "); // prefix rỗng không được xoá cả bucket

        Assert.Equal(new[] { "aquahome/dev/articles/2/cover.jpg" }, storage.Objects.Keys);
    }

    [Fact]
    public async Task List_returns_matching_objects_with_size_and_write_time()
    {
        var at = new DateTimeOffset(2026, 11, 21, 8, 0, 0, TimeSpan.Zero);
        var storage = new InMemoryStorageService(time: new FixedTime(at));
        await storage.PutObjectAsync("community/1/cover.jpg", [1, 2], "image/jpeg");
        await storage.PutObjectAsync("100/a.jpg", [1], "image/jpeg");

        var listed = await storage.ListAsync("community/");

        Assert.Equal(new[] { new StoredObjectInfo("community/1/cover.jpg", 2, at) }, listed);
        Assert.Empty(await storage.ListAsync(""));
        Assert.Empty(await new InMemoryStorageService(configured: false).ListAsync("community/"));
    }

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return now;
        }
    }

    [Fact]
    public async Task Presigned_urls_identify_the_key()
    {
        var storage = new InMemoryStorageService();

        Assert.Equal(InMemoryStorageService.UrlFor("x/y.png"), await storage.GetPresignedUrlAsync("x/y.png"));
        Assert.NotNull(await storage.GeneratePresignedPutUrlAsync("x/y.png", "image/png"));
    }

    [Fact]
    public async Task Unconfigured_storage_returns_nothing_but_refuses_writes()
    {
        IStorageService storage = new InMemoryStorageService(configured: false);

        Assert.False(storage.IsConfigured);
        Assert.Null(await storage.GetPresignedUrlAsync("x"));
        Assert.Null(await storage.GeneratePresignedPutUrlAsync("x", "image/png"));
        Assert.Null(await storage.GetObjectSizeAsync("x"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => storage.PutObjectAsync("x", [1], "image/png"));
    }
}
