using AquaHome.Common.Enums;
using AquaHome.Domain.Contests;
using AquaHome.Domain.Exceptions;
using AquaHome.EFCore.Entity;
using FishLover.Storage;

namespace AquaHome.Tests;

/// <summary>Bảng chuyển trạng thái bài dự thi — thuần, không repo, không storage.</summary>
public sealed class ContestEntryLifecycleTests
{
    private const string Key = "aquahome/test/contests/video.mp4";
    private static readonly DateTime Now = new(2026, 11, 26, 12, 0, 0, DateTimeKind.Utc);

    private static ContestEntry Entry(ContestEntryStatus status, string? key = Key, bool verified = false, TimeSpan? age = null)
        => new()
        {
            Id = Guid.NewGuid(),
            Status = (int)status,
            VideoR2Key = key,
            VideoVerifiedAt = verified ? Now.AddMinutes(-1) : null,
            SubmittedAt = Now - (age ?? TimeSpan.FromMinutes(5)),
        };

    private static Contest ContestIn(ContestStatus status) => new() { Id = Guid.NewGuid(), Title = "Cuộc thi", Status = (int)status };

    [Theory]
    [InlineData(ContestEntryStatus.Pending, true)]
    [InlineData(ContestEntryStatus.Queued, true)]
    [InlineData(ContestEntryStatus.UploadedDraft, true)]
    [InlineData(ContestEntryStatus.Published, true)]
    [InlineData(ContestEntryStatus.Rejected, false)]
    [InlineData(ContestEntryStatus.Failed, false)]
    public void Only_rejected_and_failed_entries_free_their_place(ContestEntryStatus status, bool active)
    {
        Assert.Equal(active, ContestEntryLifecycle.IsActive(Entry(status)));
    }

    [Fact]
    public void Only_a_pending_entry_with_a_video_key_can_be_confirmed()
    {
        Assert.True(ContestEntryLifecycle.CanConfirm(Entry(ContestEntryStatus.Pending)));
        Assert.False(ContestEntryLifecycle.CanConfirm(Entry(ContestEntryStatus.Pending, key: null)));
        Assert.False(ContestEntryLifecycle.CanConfirm(Entry(ContestEntryStatus.Queued)));
    }

    [Fact]
    public void Starting_a_check_queues_the_entry_and_forgets_an_old_verification()
    {
        var entry = Entry(ContestEntryStatus.Pending, verified: true);

        ContestEntryLifecycle.StartCheck(entry);

        Assert.Equal((int)ContestEntryStatus.Queued, entry.Status);
        Assert.Null(entry.VideoVerifiedAt);
    }

    [Fact]
    public void A_matching_file_stays_queued_with_its_staging_video()
    {
        var entry = Entry(ContestEntryStatus.Queued);

        ContestEntryLifecycle.FileChecked(entry, new UploadVerdict(UploadVerdictKind.Ok, 100), Now);

        Assert.Equal((int)ContestEntryStatus.Queued, entry.Status);
        Assert.Equal(Now, entry.VideoVerifiedAt);
        Assert.Equal(Key, entry.VideoR2Key);
    }

    [Theory]
    [InlineData(UploadVerdictKind.NotUploaded, "Không tìm thấy video")]
    [InlineData(UploadVerdictKind.Mismatch, "không khớp dung lượng")]
    [InlineData(UploadVerdictKind.TooLarge, "không khớp dung lượng")]
    public void A_bad_file_fails_the_entry_not_rejects_it(UploadVerdictKind kind, string reason)
    {
        var entry = Entry(ContestEntryStatus.Queued);

        ContestEntryLifecycle.FileChecked(entry, new UploadVerdict(kind, null), Now);

        Assert.Equal((int)ContestEntryStatus.Failed, entry.Status);
        Assert.Contains(reason, entry.RejectionReason);
        Assert.Null(entry.VideoR2Key);
    }

    [Fact]
    public void Storage_trouble_during_the_check_sends_the_entry_back_to_pending()
    {
        var entry = Entry(ContestEntryStatus.Queued);

        ContestEntryLifecycle.CheckUnavailable(entry);

        Assert.Equal((int)ContestEntryStatus.Pending, entry.Status);
        Assert.Equal(Key, entry.VideoR2Key);
    }

    [Fact]
    public void Posting_moves_to_review_and_hands_back_the_staging_key()
    {
        var entry = Entry(ContestEntryStatus.Queued, verified: true);

        var staging = ContestEntryLifecycle.Posted(entry, "yt-id");

        Assert.Equal(Key, staging);
        Assert.Equal((int)ContestEntryStatus.UploadedDraft, entry.Status);
        Assert.Equal("yt-id", entry.YouTubeVideoId);
        Assert.Null(entry.VideoR2Key);
    }

    [Fact]
    public void A_failed_post_fails_the_entry_and_hands_back_the_staging_key()
    {
        var entry = Entry(ContestEntryStatus.Queued, verified: true);

        var staging = ContestEntryLifecycle.PostFailed(entry);

        Assert.Equal(Key, staging);
        Assert.Equal((int)ContestEntryStatus.Failed, entry.Status);
        Assert.Equal(ContestEntryLifecycle.PostFailedReason, entry.RejectionReason);
    }

    [Fact]
    public void A_step_called_in_the_wrong_state_is_a_bug()
    {
        Assert.Throws<InvalidOperationException>(() => ContestEntryLifecycle.Posted(Entry(ContestEntryStatus.Pending), "yt"));
        Assert.Throws<InvalidOperationException>(() => ContestEntryLifecycle.StartCheck(Entry(ContestEntryStatus.Queued)));
        Assert.Throws<InvalidOperationException>(() => ContestEntryLifecycle.Publish(Entry(ContestEntryStatus.Queued)));
    }

    [Fact]
    public void Queued_entries_wait_while_the_contest_is_not_open()
    {
        var entry = Entry(ContestEntryStatus.Queued, verified: true);

        Assert.True(ContestEntryLifecycle.IsReadyToPost(entry, ContestIn(ContestStatus.Active)));
        Assert.False(ContestEntryLifecycle.IsReadyToPost(entry, ContestIn(ContestStatus.Draft)));
        Assert.False(ContestEntryLifecycle.IsReadyToPost(Entry(ContestEntryStatus.Queued), ContestIn(ContestStatus.Active)));
    }

    [Fact]
    public void Approval_needs_an_entry_awaiting_review_in_a_contest_that_has_not_ended()
    {
        var waiting = Entry(ContestEntryStatus.UploadedDraft, key: null);
        waiting.YouTubeVideoId = "yt";

        ContestEntryLifecycle.EnsureCanApprove(waiting, ContestIn(ContestStatus.Active));
        Assert.Throws<ContestValidationException>(() => ContestEntryLifecycle.EnsureCanApprove(waiting, ContestIn(ContestStatus.Ended)));
        Assert.Throws<ContestValidationException>(() =>
            ContestEntryLifecycle.EnsureCanApprove(Entry(ContestEntryStatus.Published), ContestIn(ContestStatus.Active)));
    }

    [Theory]
    [InlineData(ContestEntryStatus.UploadedDraft, true)]
    [InlineData(ContestEntryStatus.Published, true)]
    [InlineData(ContestEntryStatus.Pending, false)]
    [InlineData(ContestEntryStatus.Queued, false)]
    [InlineData(ContestEntryStatus.Failed, false)]
    public void Only_entries_already_on_YouTube_can_be_rejected(ContestEntryStatus status, bool allowed)
    {
        var entry = Entry(status, key: null);
        void Check() => ContestEntryLifecycle.EnsureCanReject(entry, ContestIn(ContestStatus.Active));

        if (allowed)
        {
            Check();
            ContestEntryLifecycle.Reject(entry, "  Video không phải bể cá  ");
            Assert.Equal((int)ContestEntryStatus.Rejected, entry.Status);
            Assert.Equal("Video không phải bể cá", entry.RejectionReason);
        }
        else
        {
            Assert.Throws<ContestValidationException>(Check);
        }
    }

    [Fact]
    public void Rejecting_after_the_contest_ended_is_refused()
    {
        Assert.Throws<ContestValidationException>(() =>
            ContestEntryLifecycle.EnsureCanReject(Entry(ContestEntryStatus.Published), ContestIn(ContestStatus.Ended)));
    }

    [Fact]
    public void A_contest_cannot_end_while_entries_are_queued_or_awaiting_review()
    {
        ContestEntryLifecycle.EnsureContestCanEnd(
            [Entry(ContestEntryStatus.Pending), Entry(ContestEntryStatus.Published), Entry(ContestEntryStatus.Failed)]);

        var ex = Assert.Throws<ContestValidationException>(() => ContestEntryLifecycle.EnsureContestCanEnd(
            [Entry(ContestEntryStatus.Queued), Entry(ContestEntryStatus.UploadedDraft), Entry(ContestEntryStatus.Published)]));
        Assert.Contains("Còn 2 bài", ex.Message);
    }

    public static TheoryData<ContestEntryStatus, bool, int, string?> StagingCases => new()
    {
        { ContestEntryStatus.Pending, false, 12, null },                                       // còn đang upload/xác nhận
        { ContestEntryStatus.Pending, false, 30, ContestEntryLifecycle.AbandonedReason },      // tải lên rồi bỏ
        { ContestEntryStatus.Pending, true, 48, null },                                        // luồng cũ: hẹn 3 ngày
        { ContestEntryStatus.Pending, true, 80, ContestEntryLifecycle.ExpiredReason },
        { ContestEntryStatus.Queued, false, 30, ContestEntryLifecycle.AbandonedReason },       // kẹt giữa lúc kiểm file
        { ContestEntryStatus.Queued, true, 24 * 30, null },                                    // hàng chờ không hết hạn
        { ContestEntryStatus.UploadedDraft, true, 24 * 30, null },
    };

    [Theory]
    [MemberData(nameof(StagingCases))]
    public void Staging_expiry(ContestEntryStatus status, bool verified, int ageHours, string? expected)
    {
        var entry = Entry(status, verified: verified, age: TimeSpan.FromHours(ageHours));

        Assert.Equal(expected, ContestEntryLifecycle.StagingExpiry(entry, Now));
    }
}
