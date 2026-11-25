using System.Security.Claims;
using FishLover.Shared.Services;

namespace AquaHome.Tests.Infrastructure;

internal sealed class FakeUser(Guid id) : ICurrentUserSession
{
    public Guid UserId => id;
    public string UserIdString => id.ToString();
    public string? UserName => "tester";
    public string? Email => null;
    public IEnumerable<Claim> Claims => [];
    public IReadOnlyList<string> Roles => [];
    public bool IsAdmin => false;
    public bool IsContentAdmin => false;
    public bool IsAuthenticated => true;
}
