using System.Security.Claims;

namespace FishLover.Shared.Services;

public interface ICurrentUserSession
{
    Guid UserId { get; }              // Guid vì bạn đã chuyển sang Guid
    string UserIdString { get; }      // Helper để get string nếu cần
    string? UserName { get; }
    string? Email { get; }
    IEnumerable<Claim> Claims { get; }
    IReadOnlyList<string> Roles { get; }
    
    // Helper methods
    bool IsAdmin { get; }
    bool IsContentAdmin { get; }
    bool IsAuthenticated { get; }
}