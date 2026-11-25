namespace FishLover.Storage.Tests;

/// <summary>Upload verification: HEAD → verdict, và object bị từ chối phải biến mất khỏi storage.</summary>
public sealed class UploadVerifierTests
{
    private const string Key = "x/file.jpg";
    private const long Max = 1000;

    private readonly InMemoryStorageService _storage = new();
    private readonly UploadVerifier _verifier;

    public UploadVerifierTests()
    {
        _verifier = new UploadVerifier(_storage);
    }

    private Task PutAsync(long bytes)
    {
        return _storage.PutObjectAsync(Key, new byte[bytes], "image/jpeg");
    }

    [Fact]
    public async Task Missing_object_is_NotUploaded_with_no_size()
    {
        var verdict = await _verifier.VerifyAsync(Key, new UploadPolicy(Max));

        Assert.Equal(UploadVerdictKind.NotUploaded, verdict.Kind);
        Assert.Null(verdict.Size);
    }

    [Fact]
    public async Task Unconfigured_storage_reads_as_NotUploaded()
    {
        var verifier = new UploadVerifier(new InMemoryStorageService(configured: false));

        var verdict = await verifier.VerifyAsync(Key, new UploadPolicy(Max));

        Assert.Equal(UploadVerdictKind.NotUploaded, verdict.Kind);
    }

    [Fact]
    public async Task Size_equal_to_max_is_Ok_and_object_is_kept()
    {
        await PutAsync(Max);

        var verdict = await _verifier.VerifyAsync(Key, new UploadPolicy(Max));

        Assert.True(verdict.IsOk);
        Assert.Equal(Max, verdict.Size);
        Assert.True(_storage.Objects.ContainsKey(Key));
    }

    [Fact]
    public async Task Size_over_max_is_TooLarge_and_object_is_deleted()
    {
        await PutAsync(Max + 1);

        var verdict = await _verifier.VerifyAsync(Key, new UploadPolicy(Max));

        Assert.Equal(UploadVerdictKind.TooLarge, verdict.Kind);
        Assert.Equal(Max + 1, verdict.Size);
        Assert.False(_storage.Objects.ContainsKey(Key));
    }

    [Fact]
    public async Task Expected_size_match_is_Ok()
    {
        await PutAsync(500);

        var verdict = await _verifier.VerifyAsync(Key, new UploadPolicy(Max, ExpectedBytes: 500));

        Assert.True(verdict.IsOk);
        Assert.True(_storage.Objects.ContainsKey(Key));
    }

    [Fact]
    public async Task Expected_size_differs_is_Mismatch_and_object_is_deleted()
    {
        await PutAsync(400);

        var verdict = await _verifier.VerifyAsync(Key, new UploadPolicy(Max, ExpectedBytes: 500));

        Assert.Equal(UploadVerdictKind.Mismatch, verdict.Kind);
        Assert.Equal(400, verdict.Size);
        Assert.False(_storage.Objects.ContainsKey(Key));
    }

    [Fact]
    public async Task Over_max_wins_over_mismatch()
    {
        await PutAsync(Max + 1);

        var verdict = await _verifier.VerifyAsync(Key, new UploadPolicy(Max, ExpectedBytes: 500));

        Assert.Equal(UploadVerdictKind.TooLarge, verdict.Kind);
    }

    [Fact]
    public async Task Without_expected_size_any_size_under_max_is_Ok()
    {
        await PutAsync(1);

        var verdict = await _verifier.VerifyAsync(Key, new UploadPolicy(Max));

        Assert.True(verdict.IsOk);
    }

    [Fact]
    public async Task Storage_failure_propagates_instead_of_becoming_a_verdict()
    {
        var verifier = new UploadVerifier(new ThrowingHeadStorage());

        await Assert.ThrowsAsync<InvalidOperationException>(() => verifier.VerifyAsync(Key, new UploadPolicy(Max)));
    }

    /// <summary>R2 trục trặc: HEAD ném chứ không trả null.</summary>
    private sealed class ThrowingHeadStorage : IStorageService
    {
        public bool IsConfigured => true;

        public Task<long?> GetObjectSizeAsync(string objectKey, CancellationToken ct = default)
        {
            throw new InvalidOperationException("R2 down");
        }

        public Task<string?> GetPresignedUrlAsync(string objectKey, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<string?> GetPresignedUrlAsync(string objectKey, TimeSpan expiry, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<string?> GeneratePresignedPutUrlAsync(string objectKey, string contentType, CancellationToken ct = default) => throw new NotSupportedException();
        public Task PutObjectAsync(string objectKey, byte[] content, string contentType, CancellationToken ct = default) => throw new NotSupportedException();
        public Task DeleteAsync(string objectKey, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<StoredObjectInfo>> ListAsync(string prefix, CancellationToken ct = default) => throw new NotSupportedException();
        public Task DeleteByPrefixAsync(string prefix, CancellationToken ct = default) => throw new NotSupportedException();
    }
}
