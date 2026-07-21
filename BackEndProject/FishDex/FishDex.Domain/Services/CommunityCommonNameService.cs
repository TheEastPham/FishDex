using FishDex.Domain.DTOs.Species;
using FishDex.Domain.Services.Interfaces;
using FishDex.EFCore.Cache;
using FishDex.EFCore.Entity.Species;
using FishDex.EFCore.Repository.Interface;
using FishLover.Shared.Services;
using Microsoft.Extensions.Logging;

namespace FishDex.Domain.Services;

/// <summary>
/// User đóng góp tên địa phương (tiếng Việt) cho loài FishBase đã tồn tại — submit + moderation.
/// Lưu thẳng vào CommonNames (ContributedBy != null, IsVerified=false). Read path đã đọc CommonNames
/// nên chỉ cần lọc IsVerified; verify xong invalidate SpeciesSnapshot để tên preferred re-flatten.
/// </summary>
public class CommunityCommonNameService(
    ICommunityCommonNameRepository repo,
    ISpeciesCache speciesCache,
    ICurrentUserSession currentUser,
    ILogger<CommunityCommonNameService> logger) : ICommunityCommonNameService
{
    private const int CommunityMinSpecCode = 500_000;
    private const int ContributedRank = 999; // xếp sau tên preferred gốc của FishBase trong cùng ngôn ngữ

    public async Task<SubmitCommonNameResult> SubmitAsync(int specCode, SubmitCommonNameRequest request, CancellationToken ct = default)
    {
        if (specCode >= CommunityMinSpecCode)
            return new SubmitCommonNameResult(SubmitCommonNameOutcome.InvalidSpecies);

        if (!await repo.SpeciesExistsAsync(specCode, ct))
            return new SubmitCommonNameResult(SubmitCommonNameOutcome.SpeciesNotFound);

        var language = NormalizeLanguage(request.Language);
        var comName = request.ComName.Trim();

        if (await repo.ExistsAsync(specCode, comName, language, ct))
            return new SubmitCommonNameResult(SubmitCommonNameOutcome.Duplicate);

        if (await repo.HasPendingByUserAsync(currentUser.UserId, specCode, language, ct))
            return new SubmitCommonNameResult(SubmitCommonNameOutcome.PendingExists);

        var name = new CommonName
        {
            SpecCode        = specCode,
            ComName         = comName,
            Language        = language,
            CountryCode     = string.IsNullOrWhiteSpace(request.CountryCode) ? null : request.CountryCode.Trim().ToUpperInvariant(),
            Transliteration = request.Transliteration?.Trim(),
            NameType        = "Vernacular",
            IsPreferred     = false,
            Rank            = ContributedRank,
            ContributedBy   = currentUser.UserId,
            IsVerified      = false,
        };

        await repo.AddAsync(name, ct);
        await repo.SaveChangesAsync(ct);
        logger.LogInformation("Community common name submitted for SpecCode {SpecCode} by {UserId}", specCode, currentUser.UserId);

        return new SubmitCommonNameResult(SubmitCommonNameOutcome.Created, ToDto(name));
    }

    public async Task<IReadOnlyList<CommunityCommonNameDto>> GetMineAsync(CancellationToken ct = default)
        => (await repo.GetByContributorAsync(currentUser.UserId, ct)).Select(ToDto).ToList();

    public async Task<IReadOnlyList<CommunityCommonNameDto>> GetPendingAsync(CancellationToken ct = default)
        => (await repo.GetPendingAsync(ct)).Select(ToDto).ToList();

    public async Task<bool> VerifyAsync(int autoCtr, CancellationToken ct = default)
    {
        var name = await repo.GetContributedByIdAsync(autoCtr, ct);
        if (name is null) return false;

        name.IsVerified      = true;
        name.RejectionReason = null;
        name.ReviewedBy      = currentUser.UserId;
        await repo.SaveChangesAsync(ct);

        // Snapshot đã bake CommonName lúc flatten — invalidate để lần đọc sau lấy tên mới.
        await speciesCache.InvalidateAsync(name.SpecCode, ct);
        logger.LogInformation("Community common name {AutoCtr} verified (SpecCode {SpecCode}) by {UserId}", autoCtr, name.SpecCode, currentUser.UserId);
        return true;
    }

    public async Task<int> VerifyBatchAsync(IReadOnlyList<int> autoCtrs, CancellationToken ct = default)
    {
        if (autoCtrs.Count == 0) return 0;

        var names = await repo.GetContributedByIdsAsync(autoCtrs, ct);
        if (names.Count == 0) return 0;

        foreach (var n in names)
        {
            n.IsVerified      = true;
            n.RejectionReason = null;
            n.ReviewedBy      = currentUser.UserId;
        }
        await repo.SaveChangesAsync(ct);

        // Invalidate 1 lần cho mỗi specCode bị ảnh hưởng (nhiều tên có thể cùng 1 loài).
        foreach (var specCode in names.Select(n => n.SpecCode).Distinct())
            await speciesCache.InvalidateAsync(specCode, ct);

        logger.LogInformation("Batch-verified {Count} community common names by {UserId}", names.Count, currentUser.UserId);
        return names.Count;
    }

    public async Task<bool> RejectAsync(int autoCtr, string reason, CancellationToken ct = default)
    {
        var name = await repo.GetContributedByIdAsync(autoCtr, ct);
        if (name is null) return false;

        name.IsVerified      = false;
        name.RejectionReason = reason;
        name.ReviewedBy      = currentUser.UserId;
        await repo.SaveChangesAsync(ct);
        logger.LogInformation("Community common name {AutoCtr} rejected by {UserId}", autoCtr, currentUser.UserId);
        return true;
    }

    private static CommunityCommonNameDto ToDto(CommonName c) => new(
        c.AutoCtr, c.SpecCode, c.ComName, c.Language, c.CountryCode, c.IsVerified, c.RejectionReason, c.ContributedBy);

    private static string NormalizeLanguage(string? lang) => lang?.Trim().ToLowerInvariant() switch
    {
        "vn" or "vi" or "vietnamese" => "Vietnamese",
        "en" or "eng" or "english"   => "English",
        null or ""                   => "Vietnamese",
        _                            => lang!.Trim(),
    };
}
