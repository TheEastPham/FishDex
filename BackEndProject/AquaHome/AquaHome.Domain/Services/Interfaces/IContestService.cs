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

    // ── Prize tiers ────────────────────────────────────────
    Task<ContestPrizeTierDto> CreatePrizeTierAsync(Guid contestId, CreatePrizeTierRequest request, CancellationToken ct = default);
    Task<ContestPrizeTierDto?> UpdatePrizeTierAsync(Guid contestId, Guid tierId, UpdatePrizeTierRequest request, CancellationToken ct = default);
    Task<bool> DeletePrizeTierAsync(Guid contestId, Guid tierId, CancellationToken ct = default);

    // ── Sponsors ───────────────────────────────────────────
    Task<ContestSponsorDto> CreateSponsorAsync(Guid contestId, CreateSponsorRequest request, CancellationToken ct = default);
    Task<ContestSponsorDto?> UpdateSponsorAsync(Guid contestId, Guid sponsorId, UpdateSponsorRequest request, CancellationToken ct = default);
    Task<bool> DeleteSponsorAsync(Guid contestId, Guid sponsorId, CancellationToken ct = default);
    Task<SponsorLogoUploadResultDto?> RequestSponsorLogoUploadAsync(Guid contestId, Guid sponsorId, string fileName, string contentType, CancellationToken ct = default);

    /// <summary>Admin chốt giải: gán PrizeTierId cho các entry đã Published, denorm xuống Snapshot, set Contest.Status=Ended.
    /// Ném ContestValidationException (422) nếu vượt SlotCount của tier hoặc entry chưa Published.</summary>
    Task<bool> FinalizeAsync(Guid contestId, FinalizeContestRequest request, CancellationToken ct = default);
}
