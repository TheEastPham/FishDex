using AquaHome.Common.Enums;
using AquaHome.Domain.Contests;
using AquaHome.Domain.Exceptions;
using AquaHome.Domain.Services.Interfaces;
using AquaHome.Domain.Settings;
using AquaHome.EFCore.Data;
using AquaHome.EFCore.Entity;
using AquaHome.EFCore.Repository;
using AquaHome.Tests.Infrastructure;
using FishLover.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AquaHome.Tests;

/// <summary>Hàng chờ đăng YouTube của Worker: repo thật trên EF InMemory, YouTube và quota giả.</summary>
public sealed class ContestUploadQueueTests : IDisposable
{
    private const int Type = (int)QuotaType.ContestYouTubeUploadPerDay;

    private readonly AquaHomeDbContext _db = new(
        new DbContextOptionsBuilder<AquaHomeDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private readonly InMemoryStorageService _storage = new();
    private readonly FakeYouTube _youTube = new();
    private readonly CountingQuotaRepo _quota = new();
    private readonly FixedClock _clock = new(new DateTimeOffset(2026, 11, 26, 20, 0, 0, TimeSpan.Zero));
    private readonly Contest _contest = new() { Id = Guid.NewGuid(), Title = "Cuộc thi mùa đông", Status = (int)ContestStatus.Active };

    public ContestUploadQueueTests()
    {
        _db.Contests.Add(_contest);
        _db.SaveChanges();
    }

    public void Dispose() => _db.Dispose();

    private YouTubeUploadAllowance Allowance(int dailyLimit = 5)
        => new(_quota, Options.Create(new YouTubeSettings { DailyUploadLimit = dailyLimit }), _clock);

    private ContestUploadQueue Queue(int dailyLimit = 5) => new(
        new ContestEntryRepository(_db),
        new AquariumSnapshotRepository(_db),
        _youTube,
        _storage,
        Allowance(dailyLimit),
        NullLogger<ContestUploadQueue>.Instance);

    private async Task<ContestEntry> QueuedAsync(Guid? userId = null, int minutesAgo = 10, string? title = "Bể của tôi")
    {
        var id = Guid.NewGuid();
        var key = $"aquahome/test/contests/{id}.mp4";
        await _storage.PutObjectAsync(key, [1, 2, 3], "video/mp4");
        var entry = new ContestEntry
        {
            Id = id,
            ContestId = _contest.Id,
            AquariumSnapshotId = Guid.NewGuid(),
            UserId = userId ?? Guid.NewGuid(),
            VideoR2Key = key,
            VideoSizeBytes = 3,
            VideoVerifiedAt = DateTime.UtcNow.AddMinutes(-minutesAgo),
            Title = title,
            Status = (int)ContestEntryStatus.Queued,
            SubmittedAt = DateTime.UtcNow.AddMinutes(-minutesAgo - 1),
        };
        _db.ContestEntries.Add(entry);
        await _db.SaveChangesAsync();
        return entry;
    }

    private async Task<bool> StagingExistsAsync(string key) => await _storage.GetObjectSizeAsync(key) is not null;

    [Fact]
    public async Task Posts_a_queued_entry_and_drops_its_staging_video()
    {
        var entry = await QueuedAsync();
        var key = entry.VideoR2Key!;

        var posted = await Queue().PostQueuedAsync();

        Assert.Equal(1, posted);
        Assert.Equal((int)ContestEntryStatus.UploadedDraft, entry.Status);
        Assert.Equal($"yt-{key}", entry.YouTubeVideoId);
        Assert.Null(entry.VideoR2Key);
        Assert.False(await StagingExistsAsync(key));
        Assert.Equal(["Bể của tôi"], _youTube.Titles);
        Assert.Equal(1, _quota.Usage(entry.UserId, Type, Allowance().Today()));
    }

    [Fact]
    public async Task Falls_back_to_the_contest_title_when_the_entry_has_none()
    {
        await QueuedAsync(title: null);

        await Queue().PostQueuedAsync();

        Assert.Equal(["Cuộc thi mùa đông"], _youTube.Titles);
    }

    [Fact]
    public async Task A_rejected_upload_fails_the_entry()
    {
        var entry = await QueuedAsync();
        var key = entry.VideoR2Key!;
        _youTube.Refuse.Add(key);

        var posted = await Queue().PostQueuedAsync();

        Assert.Equal(0, posted);
        Assert.Equal((int)ContestEntryStatus.Failed, entry.Status);
        Assert.Equal(ContestEntryLifecycle.PostFailedReason, entry.RejectionReason);
        Assert.False(await StagingExistsAsync(key));
    }

    [Fact]
    public async Task YouTube_quota_exhaustion_keeps_the_whole_queue_waiting()
    {
        var first = await QueuedAsync(minutesAgo: 20);
        var second = await QueuedAsync(minutesAgo: 10);
        _youTube.QuotaExhausted = true;

        var posted = await Queue().PostQueuedAsync();

        Assert.Equal(0, posted);
        Assert.Equal((int)ContestEntryStatus.Queued, first.Status);
        Assert.Equal((int)ContestEntryStatus.Queued, second.Status);
        Assert.True(await StagingExistsAsync(first.VideoR2Key!));
        Assert.Single(_youTube.Titles); // dừng ngay sau lần đầu YouTube báo hết quota
    }

    [Fact]
    public async Task Stops_when_the_system_allowance_is_used_up()
    {
        var entries = new[] { await QueuedAsync(minutesAgo: 30), await QueuedAsync(minutesAgo: 20), await QueuedAsync(minutesAgo: 10) };

        var posted = await Queue(dailyLimit: 2).PostQueuedAsync();

        Assert.Equal(2, posted);
        Assert.Equal((int)ContestEntryStatus.UploadedDraft, entries[0].Status);
        Assert.Equal((int)ContestEntryStatus.UploadedDraft, entries[1].Status);
        Assert.Equal((int)ContestEntryStatus.Queued, entries[2].Status); // bài nộp sau đợi ngày mai
    }

    [Fact]
    public async Task A_user_over_their_own_allowance_waits_while_others_go_ahead()
    {
        var busy = Guid.NewGuid();
        var a = await QueuedAsync(busy, minutesAgo: 40);
        var b = await QueuedAsync(busy, minutesAgo: 30);
        var c = await QueuedAsync(busy, minutesAgo: 20);
        var other = await QueuedAsync(minutesAgo: 10);

        var posted = await Queue().PostQueuedAsync();

        Assert.Equal(3, posted);
        Assert.Equal((int)ContestEntryStatus.UploadedDraft, a.Status);
        Assert.Equal((int)ContestEntryStatus.UploadedDraft, b.Status);
        Assert.Equal((int)ContestEntryStatus.Queued, c.Status);
        Assert.Equal((int)ContestEntryStatus.UploadedDraft, other.Status);
    }

    [Fact]
    public async Task Entries_of_a_paused_contest_stay_queued()
    {
        var entry = await QueuedAsync();
        _contest.Status = (int)ContestStatus.Draft;
        await _db.SaveChangesAsync();

        Assert.Equal(0, await Queue().PostQueuedAsync());
        Assert.Equal((int)ContestEntryStatus.Queued, entry.Status);
        Assert.Empty(_youTube.Titles);
    }

    [Fact]
    public async Task An_entry_still_being_checked_is_not_picked_up()
    {
        var entry = await QueuedAsync();
        entry.VideoVerifiedAt = null; // request xác nhận đang HEAD file
        await _db.SaveChangesAsync();

        Assert.Equal(0, await Queue().PostQueuedAsync());
        Assert.Empty(_youTube.Titles);
    }

    private sealed class FakeYouTube : IYouTubeUploadService
    {
        public List<string> Titles { get; } = [];
        public HashSet<string> Refuse { get; } = [];
        public bool QuotaExhausted { get; set; }

        public Task<string?> UploadUnlistedAsync(string objectKey, string title, string description, CancellationToken ct = default)
        {
            Titles.Add(title);
            if (QuotaExhausted) throw new YouTubeQuotaExceededException("quotaExceeded");
            return Task.FromResult(Refuse.Contains(objectKey) ? null : $"yt-{objectKey}");
        }

        public Task SetPublicAsync(string youTubeVideoId, string? playlistId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task DeleteVideoAsync(string youTubeVideoId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<string, long>> GetViewCountsAsync(IReadOnlyList<string> videoIds, CancellationToken ct = default) => throw new NotSupportedException();
    }
}
