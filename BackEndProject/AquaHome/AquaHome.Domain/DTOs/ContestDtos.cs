using AquaHome.Domain.Enums;

namespace AquaHome.Domain.DTOs;

public record ContestDto(
    Guid Id,
    string Title,
    string? Description,
    string? YouTubePlaylistId,
    DateTime StartAt,
    DateTime EndAt,
    ContestStatus Status,
    IReadOnlyList<ContestPrizeTierDto> PrizeTiers,
    IReadOnlyList<ContestSponsorDto> Sponsors);

// ── Prize tiers ────────────────────────────────────────────
public record ContestPrizeTierDto(
    Guid Id,
    string Name,
    PrizeTierLevel TierLevel,
    int SlotCount,
    int DisplayOrder,
    string? Description,
    string? ImageUrl);

public record CreatePrizeTierRequest(string Name, PrizeTierLevel TierLevel, int SlotCount, string? Description);
public record UpdatePrizeTierRequest(string? Name, PrizeTierLevel? TierLevel, int? SlotCount, int? DisplayOrder, string? Description);
public record PrizeTierImageUploadResultDto(string UploadUrl, string ObjectKey);

// ── Sponsors ───────────────────────────────────────────────
public record ContestSponsorDto(
    Guid Id,
    string Name,
    string? WebsiteUrl, // website hoặc Facebook Page — link chung, không phân biệt loại
    string? Address,
    string? LogoUrl,
    SponsorTier SponsorTier,
    int DisplayOrder);

public record CreateSponsorRequest(string Name, string? WebsiteUrl, string? Address, SponsorTier SponsorTier);
public record UpdateSponsorRequest(string? Name, string? WebsiteUrl, string? Address, SponsorTier? SponsorTier, int? DisplayOrder, bool? IsActive);
public record SponsorLogoUploadResultDto(string UploadUrl, string ObjectKey);

// ── Finalize ───────────────────────────────────────────────
public record EntryAwardAssignment(Guid EntryId, Guid? PrizeTierId);
public record FinalizeContestRequest(IReadOnlyList<EntryAwardAssignment> Assignments);

public record CreateContestRequest(
    string Title,
    string? Description,
    string? YouTubePlaylistId,
    DateTime StartAt,
    DateTime EndAt);

public record UpdateContestRequest(
    string? Title,
    string? Description,
    string? YouTubePlaylistId,
    DateTime? StartAt,
    DateTime? EndAt,
    ContestStatus? Status);

public record SubmitEntryRequest(
    Guid AquariumSnapshotId,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    int VideoDurationSeconds,
    // Tên video trên YouTube — để trống thì lấy tên bể đã public, cuối cùng mới fallback tên contest.
    string? Title = null,
    // Mô tả do người dự thi tự viết, tối đa 100 ký tự.
    string? Description = null);

public record RejectEntryRequest(string Reason);

public record ContestEntryDto(
    Guid Id,
    Guid ContestId,
    Guid AquariumSnapshotId,
    string? YouTubeVideoId,
    long YouTubeViewCount,
    Guid? PrizeTierId,
    string? PrizeTierName,
    ContestEntryStatus Status,
    DateTime SubmittedAt,
    string? Title,
    string? Description,
    string? RejectionReason,
    // Denorm từ snapshot để admin duyệt / user theo dõi nhìn được nội dung thật, không phải chỉ ngày nộp
    string? AquariumName,
    string? OwnerNickname,
    string? SnapshotSlug);

public record SubmitEntryResultDto(Guid EntryId, string UploadUrl, string ObjectKey);

public record LeaderboardEntryDto(
    Guid EntryId,
    Guid AquariumSnapshotId,
    string? YouTubeVideoId,
    long YouTubeViewCount,
    Guid? PrizeTierId,
    string? PrizeTierName,
    PrizeTierLevel? PrizeTierLevel);
