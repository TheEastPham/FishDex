using AquaHome.Domain.Enums;

namespace AquaHome.Domain.DTOs;

public record ContestDto(
    Guid Id,
    string Title,
    string? Description,
    string? YouTubePlaylistId,
    DateTime StartAt,
    DateTime EndAt,
    ContestStatus Status);

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
    int? Rank,
    ContestEntryStatus Status,
    DateTime SubmittedAt);

public record SubmitEntryResultDto(Guid EntryId, string UploadUrl, string ObjectKey);

public record LeaderboardEntryDto(
    Guid EntryId,
    Guid AquariumSnapshotId,
    string? YouTubeVideoId,
    long YouTubeViewCount,
    int? Rank);
