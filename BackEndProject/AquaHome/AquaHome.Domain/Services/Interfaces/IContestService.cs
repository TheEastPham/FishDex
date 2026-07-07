using AquaHome.Domain.DTOs;

namespace AquaHome.Domain.Services.Interfaces;

public interface IContestService
{
    Task<IReadOnlyList<ContestDto>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ContestDto>> GetActiveAsync(CancellationToken ct = default);
    Task<ContestDto> CreateAsync(CreateContestRequest request, CancellationToken ct = default);
    Task<ContestDto?> UpdateAsync(Guid id, UpdateContestRequest request, CancellationToken ct = default);

    /// <summary>Presigned PUT URL cho video lên R2. Ném ContestValidationException (422) hoặc StorageOverloadedException (503).</summary>
    Task<SubmitEntryResultDto> SubmitEntryAsync(Guid contestId, SubmitEntryRequest request, CancellationToken ct = default);

    /// <summary>FE gọi sau khi PUT video xong lên R2. Auto-validate + upload YouTube Unlisted → Status=UploadedDraft.</summary>
    Task<bool> ConfirmUploadAsync(Guid contestId, Guid entryId, CancellationToken ct = default);

    Task<bool> ApproveEntryAsync(Guid contestId, Guid entryId, CancellationToken ct = default);
    Task<bool> RejectEntryAsync(Guid contestId, Guid entryId, CancellationToken ct = default);
    Task<IReadOnlyList<LeaderboardEntryDto>> GetLeaderboardAsync(Guid contestId, CancellationToken ct = default);
    Task<IReadOnlyList<ContestEntryDto>> GetPendingReviewAsync(CancellationToken ct = default);
}
