using AquaHome.Common.Enums;
using AquaHome.Domain.Exceptions;
using AquaHome.Domain.Services;
using AquaHome.Domain.Services.Interfaces;
using AquaHome.Domain.Storage;
using AquaHome.EFCore.Entity;
using AquaHome.EFCore.Repository.Interface;
using AquaHome.Tests.Infrastructure;
using FishLover.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace AquaHome.Tests;

/// <summary>
/// Khoá hành vi ContestService.ConfirmUploadAsync: kiểm file thật rồi đưa vào hàng chờ, không gọi YouTube (Worker
/// đăng sau, xem ContestUploadQueueTests). Repo là fake viết tay vì TryClaimForCheckAsync dùng
/// ExecuteUpdateAsync — EF InMemory không hỗ trợ. Storage là fake báo dung lượng tuỳ ý để khỏi cấp phát 500 MB.
/// </summary>
public sealed class ContestConfirmUploadTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    // Trần video hiện hành (ContestService.MaxVideoUploadBytes = 500 MB).
    private const long MaxVideoBytes = 500L * 1024 * 1024;

    private readonly FakeEntryRepo _entries = new();
    private readonly FixedSizeStorage _storage = new();
    private readonly FakeYouTube _youTube = new();
    private readonly FakeTierRepo _tiers = new();
    private readonly FakeSponsorRepo _sponsors = new();
    private readonly Contest _contest = new()
    {
        Id = Guid.NewGuid(),
        Title = "Cuộc thi",
        Status = (int)ContestStatus.Active,
        StartAt = DateTime.UtcNow.AddDays(-1),
        EndAt = DateTime.UtcNow.AddDays(1),
    };

    private ContestService Service()
    {
        return new ContestService(
            new FakeContestRepo(_contest),
            _entries,
            _tiers,
            _sponsors,
            new FakeSnapshotRepo(),
            _storage,
            new UploadVerifier(_storage),
            _youTube,
            new FakeUser(UserId),
            new AquaHomeObjectKeys("test"),
            NullLogger<ContestService>.Instance);
    }

    private ContestEntry SeedEntry(long declaredBytes, long? actualBytes)
    {
        var entry = new ContestEntry
        {
            Id = Guid.NewGuid(),
            ContestId = _contest.Id,
            AquariumSnapshotId = Guid.NewGuid(),
            UserId = UserId,
            VideoR2Key = "aquahome/test/contests/video.mp4",
            VideoSizeBytes = declaredBytes,
            Status = (int)ContestEntryStatus.Pending,
            SubmittedAt = DateTime.UtcNow,
        };
        _entries.Items.Add(entry);
        if (actualBytes is { } size)
        {
            _storage.Sizes[entry.VideoR2Key] = size;
        }

        return entry;
    }

    [Fact]
    public async Task No_file_fails_the_entry_as_not_found()
    {
        var entry = SeedEntry(declaredBytes: 100, actualBytes: null);

        var ok = await Service().ConfirmUploadAsync(_contest.Id, entry.Id);

        Assert.False(ok);
        Assert.Equal((int)ContestEntryStatus.Failed, entry.Status);
        Assert.Contains("Không tìm thấy video", entry.RejectionReason);
        Assert.Null(entry.VideoR2Key);
        Assert.Empty(_storage.Deleted); // không có gì để xoá
    }

    [Fact]
    public async Task Size_different_from_declared_fails_the_entry_and_object_deleted()
    {
        var entry = SeedEntry(declaredBytes: 100, actualBytes: 50);
        var key = entry.VideoR2Key!;

        var ok = await Service().ConfirmUploadAsync(_contest.Id, entry.Id);

        Assert.False(ok);
        Assert.Equal((int)ContestEntryStatus.Failed, entry.Status);
        Assert.Contains("không khớp dung lượng", entry.RejectionReason);
        Assert.Null(entry.VideoR2Key);
        Assert.Equal([key], _storage.Deleted);
    }

    [Fact]
    public async Task Size_over_the_limit_fails_with_the_same_reason_and_object_deleted()
    {
        var entry = SeedEntry(declaredBytes: MaxVideoBytes + 1, actualBytes: MaxVideoBytes + 1);
        var key = entry.VideoR2Key!;

        var ok = await Service().ConfirmUploadAsync(_contest.Id, entry.Id);

        Assert.False(ok);
        Assert.Equal((int)ContestEntryStatus.Failed, entry.Status);
        Assert.Contains("không khớp dung lượng", entry.RejectionReason);
        Assert.Equal([key], _storage.Deleted);
    }

    [Fact]
    public async Task Storage_failure_returns_the_entry_to_Pending_and_rethrows()
    {
        var entry = SeedEntry(declaredBytes: 100, actualBytes: 100);
        var key = entry.VideoR2Key!;
        _storage.ThrowOnHead = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() => Service().ConfirmUploadAsync(_contest.Id, entry.Id));

        Assert.Equal((int)ContestEntryStatus.Pending, entry.Status);
        Assert.Equal(key, entry.VideoR2Key); // R2 lỗi chứ không phải lỗi bài: giữ nguyên để xác nhận lại
        Assert.Empty(_storage.Deleted);
    }

    [Fact]
    public async Task Matching_file_is_queued_for_the_worker_with_its_staging_copy_kept()
    {
        var entry = SeedEntry(declaredBytes: 100, actualBytes: 100);
        var key = entry.VideoR2Key!;

        var ok = await Service().ConfirmUploadAsync(_contest.Id, entry.Id);

        Assert.True(ok);
        Assert.Equal((int)ContestEntryStatus.Queued, entry.Status);
        Assert.NotNull(entry.VideoVerifiedAt);
        Assert.Equal(key, entry.VideoR2Key);
        Assert.Empty(_storage.Deleted);
        Assert.Null(_youTube.UploadedKey); // request xác nhận không còn đăng YouTube
    }

    [Fact]
    public async Task An_entry_already_in_the_queue_cannot_be_confirmed_again()
    {
        var entry = SeedEntry(declaredBytes: 100, actualBytes: 100);
        entry.Status = (int)ContestEntryStatus.Queued;

        Assert.False(await Service().ConfirmUploadAsync(_contest.Id, entry.Id));
    }

    // ── Logo sponsor / ảnh hạng giải: confirm kiểm file thật, trần 300 KB ───────

    private const long MaxLogoBytes = 300L * 1024;

    private ContestPrizeTier SeedTier(long? actualBytes, string? key = "aquahome/test/contests/tier.png")
    {
        var tier = new ContestPrizeTier { Id = Guid.NewGuid(), ContestId = _contest.Id, Name = "Giải Nhất", ImageObjectKey = key };
        _tiers.Items.Add(tier);
        if (key is not null && actualBytes is { } size)
        {
            _storage.Sizes[key] = size;
        }

        return tier;
    }

    private ContestSponsor SeedSponsor(long? actualBytes, string? key = "aquahome/test/contests/sponsor.png")
    {
        var sponsor = new ContestSponsor { Id = Guid.NewGuid(), ContestId = _contest.Id, Name = "Nhà tài trợ", LogoObjectKey = key };
        _sponsors.Items.Add(sponsor);
        if (key is not null && actualBytes is { } size)
        {
            _storage.Sizes[key] = size;
        }

        return sponsor;
    }

    [Fact]
    public async Task Prize_tier_image_within_the_limit_is_confirmed_and_kept()
    {
        var tier = SeedTier(actualBytes: MaxLogoBytes); // đúng bằng trần vẫn hợp lệ

        var ok = await Service().ConfirmPrizeTierImageUploadAsync(_contest.Id, tier.Id);

        Assert.True(ok);
        Assert.NotNull(tier.ImageObjectKey);
        Assert.Empty(_storage.Deleted);
    }

    [Fact]
    public async Task Prize_tier_image_over_the_limit_is_detached_deleted_and_rejected()
    {
        var tier = SeedTier(actualBytes: MaxLogoBytes + 1);
        var key = tier.ImageObjectKey!;

        var ex = await Assert.ThrowsAsync<ContestValidationException>(() => Service().ConfirmPrizeTierImageUploadAsync(_contest.Id, tier.Id));

        Assert.Contains("300KB", ex.Message);
        Assert.Null(tier.ImageObjectKey);
        Assert.Equal([key], _storage.Deleted);
    }

    [Fact]
    public async Task Prize_tier_image_that_was_never_uploaded_is_detached_and_rejected()
    {
        var tier = SeedTier(actualBytes: null);

        var ex = await Assert.ThrowsAsync<ContestValidationException>(() => Service().ConfirmPrizeTierImageUploadAsync(_contest.Id, tier.Id));

        Assert.Contains("Không tìm thấy ảnh", ex.Message);
        Assert.Null(tier.ImageObjectKey);
    }

    [Fact]
    public async Task Prize_tier_confirm_returns_false_when_tier_is_unknown_or_has_no_pending_upload()
    {
        var noKey = SeedTier(actualBytes: null, key: null);

        Assert.False(await Service().ConfirmPrizeTierImageUploadAsync(_contest.Id, noKey.Id));
        Assert.False(await Service().ConfirmPrizeTierImageUploadAsync(_contest.Id, Guid.NewGuid()));
        Assert.False(await Service().ConfirmPrizeTierImageUploadAsync(Guid.NewGuid(), noKey.Id)); // sai contest
    }

    [Fact]
    public async Task Sponsor_logo_within_the_limit_is_confirmed_and_kept()
    {
        var sponsor = SeedSponsor(actualBytes: 200 * 1024);

        var ok = await Service().ConfirmSponsorLogoUploadAsync(_contest.Id, sponsor.Id);

        Assert.True(ok);
        Assert.NotNull(sponsor.LogoObjectKey);
    }

    [Fact]
    public async Task Sponsor_logo_over_the_limit_is_detached_deleted_and_rejected()
    {
        var sponsor = SeedSponsor(actualBytes: MaxLogoBytes + 1);
        var key = sponsor.LogoObjectKey!;

        await Assert.ThrowsAsync<ContestValidationException>(() => Service().ConfirmSponsorLogoUploadAsync(_contest.Id, sponsor.Id));

        Assert.Null(sponsor.LogoObjectKey);
        Assert.Equal([key], _storage.Deleted);
    }

    [Fact]
    public async Task Sponsor_confirm_returns_false_for_wrong_contest()
    {
        var sponsor = SeedSponsor(actualBytes: 100);

        Assert.False(await Service().ConfirmSponsorLogoUploadAsync(Guid.NewGuid(), sponsor.Id));
    }

    // ── Đuôi key suy từ content-type, không theo tên file client gửi ────────────

    [Fact]
    public async Task Sponsor_logo_key_extension_comes_from_content_type_not_file_name()
    {
        var sponsor = SeedSponsor(actualBytes: null, key: null);

        var result = await Service().RequestSponsorLogoUploadAsync(_contest.Id, sponsor.Id, "evil.html", "image/png");

        Assert.NotNull(result);
        Assert.EndsWith(".png", result.ObjectKey);
        Assert.Equal(result.ObjectKey, sponsor.LogoObjectKey);
    }

    [Fact]
    public async Task Prize_tier_image_key_extension_comes_from_content_type_not_file_name()
    {
        var tier = SeedTier(actualBytes: null, key: null);

        var result = await Service().RequestPrizeTierImageUploadAsync(_contest.Id, tier.Id, "x.exe", "image/svg+xml");

        Assert.NotNull(result);
        Assert.EndsWith(".svg", result.ObjectKey);
        Assert.Equal(result.ObjectKey, tier.ImageObjectKey);
    }

    [Fact]
    public async Task Logo_upload_with_an_unsupported_content_type_is_refused()
    {
        var sponsor = SeedSponsor(actualBytes: null, key: null);

        var result = await Service().RequestSponsorLogoUploadAsync(_contest.Id, sponsor.Id, "a.pdf", "application/pdf");

        Assert.Null(result);
        Assert.Null(sponsor.LogoObjectKey);
    }

    // ── Fakes ───────────────────────────────────────────────────────────────

    private sealed class FakeTierRepo : IContestPrizeTierRepository
    {
        public List<ContestPrizeTier> Items { get; } = [];

        public Task<ContestPrizeTier?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(Items.FirstOrDefault(t => t.Id == id));

        public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;

        public Task<IReadOnlyList<ContestPrizeTier>> GetByContestAsync(Guid contestId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task AddAsync(ContestPrizeTier tier, CancellationToken ct = default) => throw new NotSupportedException();
        public void Remove(ContestPrizeTier tier) => throw new NotSupportedException();
    }

    private sealed class FakeSponsorRepo : IContestSponsorRepository
    {
        public List<ContestSponsor> Items { get; } = [];

        public Task<ContestSponsor?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(Items.FirstOrDefault(s => s.Id == id));

        public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;

        public Task<IReadOnlyList<ContestSponsor>> GetByContestAsync(Guid contestId, bool activeOnly, CancellationToken ct = default) => throw new NotSupportedException();
        public Task AddAsync(ContestSponsor sponsor, CancellationToken ct = default) => throw new NotSupportedException();
        public void Remove(ContestSponsor sponsor) => throw new NotSupportedException();
    }

    private sealed class FixedSizeStorage : IStorageService
    {
        public Dictionary<string, long> Sizes { get; } = [];
        public List<string> Deleted { get; } = [];
        public bool ThrowOnHead { get; set; }

        public bool IsConfigured => true;

        public Task<long?> GetObjectSizeAsync(string objectKey, CancellationToken ct = default)
        {
            if (ThrowOnHead)
            {
                throw new InvalidOperationException("R2 down");
            }

            return Task.FromResult<long?>(Sizes.TryGetValue(objectKey, out var size) ? size : null);
        }

        public Task DeleteAsync(string objectKey, CancellationToken ct = default)
        {
            Deleted.Add(objectKey);
            Sizes.Remove(objectKey);
            return Task.CompletedTask;
        }

        public Task<string?> GetPresignedUrlAsync(string objectKey, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<string?> GetPresignedUrlAsync(string objectKey, TimeSpan expiry, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<string?> GeneratePresignedPutUrlAsync(string objectKey, string contentType, CancellationToken ct = default)
            => Task.FromResult<string?>($"memory://{objectKey}?upload");

        public Task PutObjectAsync(string objectKey, byte[] content, string contentType, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<StoredObjectInfo>> ListAsync(string prefix, CancellationToken ct = default) => throw new NotSupportedException();
        public Task DeleteByPrefixAsync(string prefix, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class FakeEntryRepo : IContestEntryRepository
    {
        public List<ContestEntry> Items { get; } = [];

        public Task<ContestEntry?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(Items.FirstOrDefault(e => e.Id == id));

        // DB thật chỉ cho đúng một request chiếm được Pending → Queued; service tự phản chiếu vào entity sau đó.
        public Task<bool> TryClaimForCheckAsync(Guid entryId, CancellationToken ct = default)
            => Task.FromResult(Items.Any(e => e.Id == entryId && e.Status == (int)ContestEntryStatus.Pending));

        public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;

        public Task<IReadOnlyList<ContestEntry>> GetByContestAsync(Guid contestId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<ContestEntry>> GetByStatusAsync(ContestEntryStatus status, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<ContestEntry>> GetByActiveContestsAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<ContestEntry>> GetByUserAndContestAsync(Guid userId, Guid contestId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<ContestEntry>> GetUploadQueueAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<ContestEntry>> GetWithStagingVideoAsync(DateTime submittedBefore, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<bool> TryExpireStagingAsync(Guid entryId, ContestEntryStatus expected, string reason, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<long> SumStagingVideoBytesAsync(DateTime unverifiedSubmittedAfter, CancellationToken ct = default) => throw new NotSupportedException();
        public Task AddAsync(ContestEntry entry, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class FakeContestRepo(Contest contest) : IContestRepository
    {
        public Task<Contest?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult<Contest?>(id == contest.Id ? contest : null);

        public Task<IReadOnlyList<Contest>> GetAllAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Contest>> GetActiveAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task AddAsync(Contest c, CancellationToken ct = default) => throw new NotSupportedException();
        public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
    }

    /// <summary>Không có snapshot: tiêu đề video rơi về tên contest, đủ để đi tới bước upload.</summary>
    private sealed class FakeSnapshotRepo : IAquariumSnapshotRepository
    {
        public Task<AquariumSnapshot?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult<AquariumSnapshot?>(null);

        public Task<AquariumSnapshot?> GetBySlugAsync(string slug, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<AquariumSnapshot>> GetActiveByAquariumAsync(Guid aquariumId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<AquariumSnapshot>> GetActiveByUserAsync(Guid userId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<AquariumSnapshot> Items, int TotalCount)> GetGalleryAsync(int? waterType, int? style, string? contest, string sort, int page, int pageSize, CancellationToken ct = default) => throw new NotSupportedException();
        public Task AddAsync(AquariumSnapshot snapshot, CancellationToken ct = default) => throw new NotSupportedException();
        public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakeYouTube : IYouTubeUploadService
    {
        public string? UploadedKey { get; private set; }

        public Task<string?> UploadUnlistedAsync(string objectKey, string title, string description, CancellationToken ct = default)
        {
            UploadedKey = objectKey;
            return Task.FromResult<string?>("yt-video-id");
        }

        public Task SetPublicAsync(string youTubeVideoId, string? playlistId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task DeleteVideoAsync(string youTubeVideoId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<string, long>> GetViewCountsAsync(IReadOnlyList<string> videoIds, CancellationToken ct = default) => throw new NotSupportedException();
    }
}
