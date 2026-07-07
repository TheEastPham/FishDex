namespace AquaHome.Domain.DTOs;

/// <summary>Giá trị -1 = không giới hạn.</summary>
public record RoleQuotaDto(
    string Role,
    int MaxFavorites,
    int MaxAquariums,
    int SearchPerDay,
    int AiQaPerDay,
    int ImageSearchPerDay,
    DateTime UpdatedAt);

public record UpdateRoleQuotaRequest(
    int MaxFavorites,
    int MaxAquariums,
    int SearchPerDay,
    int AiQaPerDay,
    int ImageSearchPerDay);
