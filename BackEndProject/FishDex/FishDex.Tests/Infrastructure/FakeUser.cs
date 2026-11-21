using System.Security.Claims;
using FishLover.Shared.Services;

namespace FishDex.Tests.Infrastructure;

/// <summary>User đang đăng nhập giả — đổi <see cref="UserId"/> để mô phỏng người khác.</summary>
public sealed class FakeUser(Guid userId) : ICurrentUserSession
{
    public Guid UserId { get; set; } = userId;
    public string UserIdString => UserId.ToString();
    public string? UserName => "tester";
    public string? Email => "tester@example.invalid";
    public IEnumerable<Claim> Claims => [];
    public IReadOnlyList<string> Roles => [];
    public bool IsAdmin => false;
    public bool IsContentAdmin => false;
    public bool IsAuthenticated => true;
}
