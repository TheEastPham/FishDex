namespace AquaHome.EFCore.Entity;

public class ContestEntry
{
    public Guid Id { get; set; }
    public Guid ContestId { get; set; }
    public Guid AquariumSnapshotId { get; set; }
    public Guid UserId { get; set; }

    public string? VideoR2Key { get; set; }       // null sau khi upload YouTube xong (R2 chỉ là staging)
    public long? VideoSizeBytes { get; set; }      // dùng cho R2 storage guard (SUM WHERE VideoR2Key IS NOT NULL)
    public int VideoDurationSeconds { get; set; }  // 120–300s, đo ở FE trước khi upload

    public string? YouTubeVideoId { get; set; }    // ID 11 ký tự, không phải full URL
    public long YouTubeViewCount { get; set; }

    public int? Rank { get; set; } // null = chưa finalize; 1/2/3 = nhất/nhì/ba
    public int Status { get; set; } // ContestEntryStatus: Pending/Validating/UploadedDraft/Published/Rejected
    public DateTime SubmittedAt { get; set; }

    public Contest Contest { get; set; } = null!;
    public AquariumSnapshot AquariumSnapshot { get; set; } = null!;
}
