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
    string? Description);

public record CreatePrizeTierRequest(string Name, PrizeTierLevel TierLevel, int SlotCount, string? Description);
public record UpdatePrizeTierRequest(string? Name, PrizeTierLevel? TierLevel, int? SlotCount, int? DisplayOrder, string? Description);

// ── Sponsors ───────────────────────────────────────────────
public record ContestSponsorDto(
    Guid Id,
    string Name,
    string? WebsiteUrl,
    string? LogoUrl,
    SponsorTier SponsorTier,
    int DisplayOrder);

public record CreateSponsorRequest(string Name, string? WebsiteUrl, SponsorTier SponsorTier);
public record UpdateSponsorRequest(string? Name, string? WebsiteUrl, SponsorTier? SponsorTier, int? DisplayOrder, bool? IsActive);
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
    int VideoDurationSeconds);

public record ContestEntryDto(
    Guid Id,
    Guid ContestId,
    Guid AquariumSnapshotId,
    string? YouTubeVideoId,
    long YouTubeViewCount,
    Guid? PrizeTierId,
    string? PrizeTierName,
    ContestEntryStatus Status,
    DateTime SubmittedAt);

public record SubmitEntryResultDto(Guid EntryId, string UploadUrl, string ObjectKey);

public record LeaderboardEntryDto(
    Guid EntryId,
    Guid AquariumSnapshotId,
    string? YouTubeVideoId,
    long YouTubeViewCount,
    Guid? PrizeTierId,
    string? PrizeTierName,
    PrizeTierLevel? PrizeTierLevel);
