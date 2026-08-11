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

    // Tên + mô tả video do người dự thi tự đặt, nhập lúc submit và dùng lại lúc confirm-upload
    // (2 request tách rời nên bắt buộc phải lưu DB). Title null → fallback tên bể → tên contest.
    public string? Title { get; set; }
    public string? Description { get; set; }

    /// <summary>Lý do admin từ chối — hiển thị lại cho người dự thi biết vì sao trượt.</summary>
    public string? RejectionReason { get; set; }

    public string? YouTubeVideoId { get; set; }    // ID 11 ký tự, không phải full URL
    public long YouTubeViewCount { get; set; }

    public Guid? PrizeTierId { get; set; } // null = chưa finalize / không đoạt giải — set lúc admin finalize contest
    public int Status { get; set; } // ContestEntryStatus: Pending/Validating/UploadedDraft/Published/Rejected
    public DateTime SubmittedAt { get; set; }

    public Contest Contest { get; set; } = null!;
    public AquariumSnapshot AquariumSnapshot { get; set; } = null!;
    public ContestPrizeTier? PrizeTier { get; set; }
}
