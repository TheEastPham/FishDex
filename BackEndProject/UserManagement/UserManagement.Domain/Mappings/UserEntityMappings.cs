using System.Text.Json;
using UserManagement.Domain.DTOs.User;
using UserManagement.Domain.Models;
using UserManagement.EFCore.Entities.User;

namespace UserManagement.Domain.Mappings;

internal static class UserEntityMappings
{
    internal static User ToUser(this UserEntity entity, IList<string>? roles = null) => new()
    {
        Id          = entity.Id,
        Email       = entity.Email ?? string.Empty,
        FirstName   = entity.FirstName,
        LastName    = entity.LastName,
        DateOfBirth = entity.DateOfBirth,
        Avatar      = entity.Avatar,
        TimeZone    = entity.TimeZone,
        Language    = entity.Language,
        CreatedAt   = entity.CreatedAt,
        UpdatedAt   = entity.UpdatedAt,
        IsActive    = entity.IsActive,
        LastLoginAt = entity.LastLoginAt,
        Profile     = entity.Profile?.ToUserProfile(),
        Roles       = roles?.ToList() ?? []
    };

    internal static UserProfile ToUserProfile(this UserProfileEntity entity) => new()
    {
        Id          = entity.Id,
        UserId      = entity.UserId,
        PhoneNumber = entity.PhoneNumber,
        Address     = entity.Address,
        Bio         = entity.Bio,
        UpdatedAt   = entity.UpdatedAt,
        Preferences = ParsePreferences(entity.Preferences)
    };

    internal static UserDto ToDto(this User user) => new(
        Id:          user.Id.ToString(),
        Email:       user.Email,
        FirstName:   user.FirstName,
        LastName:    user.LastName,
        FullName:    user.FullName,
        DateOfBirth: user.DateOfBirth,
        Avatar:      user.Avatar,
        TimeZone:    user.TimeZone,
        Language:    user.Language,
        CreatedAt:   user.CreatedAt,
        UpdatedAt:   user.UpdatedAt,
        IsActive:    user.IsActive,
        LastLoginAt: user.LastLoginAt,
        Roles:       user.Roles
    );

    private static Dictionary<string, object> ParsePreferences(string? json)
    {
        if (string.IsNullOrEmpty(json)) return [];
        try { return JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? []; }
        catch { return []; }
    }
}
