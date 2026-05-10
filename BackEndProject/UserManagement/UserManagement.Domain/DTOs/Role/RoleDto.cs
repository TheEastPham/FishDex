namespace UserManagement.Domain.DTOs.Role;

public record RoleDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    DateTime CreatedAt
);
