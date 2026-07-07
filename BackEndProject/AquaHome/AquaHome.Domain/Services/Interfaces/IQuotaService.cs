using AquaHome.Common.Enums;
using AquaHome.Domain.DTOs;

namespace AquaHome.Domain.Services.Interfaces;

public interface IQuotaService
{
    /// <summary>Quota hiệu lực của role (cache 5 phút). Role không có row → fallback "Member".</summary>
    Task<RoleQuotaDto> GetQuotaAsync(string role, CancellationToken ct = default);

    /// <summary>
    /// Check quota count-based (MaxAquariums/MaxFavorites) cho user hiện tại
    /// — ném <see cref="Exceptions.QuotaExceededException"/> nếu đã chạm giới hạn.
    /// Gọi TRƯỚC khi CREATE.
    /// </summary>
    Task EnforceCountLimitAsync(QuotaType quotaType, CancellationToken ct = default);

    /// <summary>
    /// Tiêu 1 lượt quota theo ngày (SearchPerDay/AiQaPerDay/ImageSearchPerDay) cho user hiện tại
    /// — ném QuotaExceededException nếu hết lượt hôm nay. Dùng cho Story 2.6 (AI Q&A) và image search.
    /// </summary>
    Task ConsumeDailyAsync(QuotaType quotaType, CancellationToken ct = default);

    // Admin
    Task<IReadOnlyList<RoleQuotaDto>> GetAllAsync(CancellationToken ct = default);
    Task<RoleQuotaDto?> UpdateAsync(string role, UpdateRoleQuotaRequest request, CancellationToken ct = default);
}
