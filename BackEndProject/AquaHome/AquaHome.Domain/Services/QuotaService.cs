using AquaHome.Common.Enums;
using AquaHome.Domain.DTOs;
using AquaHome.Domain.Exceptions;
using AquaHome.Domain.Services.Interfaces;
using AquaHome.EFCore.Entity;
using AquaHome.EFCore.Repository.Interface;
using FishLover.Shared.Services;
using Microsoft.Extensions.Caching.Memory;

namespace AquaHome.Domain.Services;

public class QuotaService(
    IQuotaRepository quotaRepo,
    ICurrentUserSession currentUser,
    IMemoryCache cache) : IQuotaService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);
    private const string FallbackRole = "Member";

    // Ưu tiên role "mạnh" nhất khi user có nhiều role
    private static readonly string[] RolePrecedence = ["SystemAdmin", "ContentAdmin", "Member"];

    public async Task<RoleQuotaDto> GetQuotaAsync(string role, CancellationToken ct = default)
    {
        var quota = await GetCachedAsync(role, ct)
                    ?? await GetCachedAsync(FallbackRole, ct)
                    ?? throw new InvalidOperationException(
                        $"RoleQuota seed missing: no row for '{role}' nor fallback '{FallbackRole}'.");
        return ToDto(quota);
    }

    public async Task EnforceCountLimitAsync(QuotaType quotaType, CancellationToken ct = default)
    {
        var quota = await GetQuotaAsync(ResolveRole(), ct);

        var (limit, current) = quotaType switch
        {
            QuotaType.MaxAquariums => (quota.MaxAquariums, await quotaRepo.CountAquariumsAsync(currentUser.UserId, ct)),
            QuotaType.MaxFavorites => (quota.MaxFavorites, await quotaRepo.CountFavoritesAsync(currentUser.UserId, ct)),
            _ => throw new ArgumentOutOfRangeException(nameof(quotaType),
                $"{quotaType} không phải quota count-based — dùng ConsumeDailyAsync."),
        };

        if (limit >= 0 && current >= limit)
            throw new QuotaExceededException(quotaType, limit, quota.Role);
    }

    public async Task ConsumeDailyAsync(QuotaType quotaType, CancellationToken ct = default)
    {
        if (quotaType is not (QuotaType.SearchPerDay or QuotaType.AiQaPerDay or QuotaType.ImageSearchPerDay))
            throw new ArgumentOutOfRangeException(nameof(quotaType),
                $"{quotaType} không phải quota theo ngày — dùng EnforceCountLimitAsync.");

        var quota = await GetQuotaAsync(ResolveRole(), ct);
        var limit = quotaType switch
        {
            QuotaType.SearchPerDay      => quota.SearchPerDay,
            QuotaType.AiQaPerDay        => quota.AiQaPerDay,
            _                           => quota.ImageSearchPerDay,
        };

        if (limit < 0) return; // unlimited — không cần đếm

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var used = await quotaRepo.GetDailyUsageAsync(currentUser.UserId, (int)quotaType, today, ct);
        if (used >= limit)
            throw new QuotaExceededException(quotaType, limit, quota.Role);

        // Increment atomic; nếu race làm vượt 1-2 lượt biên thì chấp nhận được (soft limit)
        var after = await quotaRepo.IncrementDailyUsageAsync(currentUser.UserId, (int)quotaType, today, ct);
        if (after > limit)
            throw new QuotaExceededException(quotaType, limit, quota.Role);
    }

    public async Task<IReadOnlyList<RoleQuotaDto>> GetAllAsync(CancellationToken ct = default)
        => (await quotaRepo.GetAllAsync(ct)).Select(ToDto).ToList();

    public async Task<RoleQuotaDto?> UpdateAsync(string role, UpdateRoleQuotaRequest request, CancellationToken ct = default)
    {
        var entity = await quotaRepo.GetByRoleAsync(role, ct);
        if (entity is null) return null;

        entity.MaxFavorites      = request.MaxFavorites;
        entity.MaxAquariums      = request.MaxAquariums;
        entity.SearchPerDay      = request.SearchPerDay;
        entity.AiQaPerDay        = request.AiQaPerDay;
        entity.ImageSearchPerDay = request.ImageSearchPerDay;
        entity.UpdatedAt         = DateTime.UtcNow;

        await quotaRepo.UpdateAsync(entity, ct);
        cache.Remove(CacheKey(role)); // thay đổi có hiệu lực ngay với instance này, tối đa 5' với cache cũ
        return ToDto(entity);
    }

    private string ResolveRole()
        => RolePrecedence.FirstOrDefault(r => currentUser.Roles.Contains(r)) ?? FallbackRole;

    private async Task<RoleQuota?> GetCachedAsync(string role, CancellationToken ct)
        => await cache.GetOrCreateAsync(CacheKey(role), entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheTtl;
            return quotaRepo.GetByRoleAsync(role, ct);
        });

    private static string CacheKey(string role) => $"quota:{role}";

    private static RoleQuotaDto ToDto(RoleQuota q) => new(
        q.Role, q.MaxFavorites, q.MaxAquariums, q.SearchPerDay, q.AiQaPerDay, q.ImageSearchPerDay, q.UpdatedAt);
}
