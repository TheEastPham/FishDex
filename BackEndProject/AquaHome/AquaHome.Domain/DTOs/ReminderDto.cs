using AquaHome.Common.Enums;

namespace AquaHome.Domain.DTOs;

public record ReminderDto(
    Guid Id,
    Guid AquariumId,
    AquaTaskType AquaTaskType,
    DateTime DueAt,
    int? IntervalDays,
    bool IsCompleted,
    DateTime? CompletedAt
);

public record CreateReminderRequest(
    AquaTaskType AquaTaskType,
    DateTime DueAt,
    int? IntervalDays
);

public record CompleteReminderResponse(
    Guid CompletedId,
    DateTime? SuggestedNextDueAt
);

public record UserReminderDto(
    Guid Id,
    Guid AquariumId,
    string AquariumName,
    AquaTaskType AquaTaskType,
    DateTime DueAt,
    int? IntervalDays,
    bool IsCompleted,
    DateTime? CompletedAt
);
