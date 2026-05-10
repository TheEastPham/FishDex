using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace FishLover.Shared.Services;

public class CurrentUserSession : ICurrentUserSession
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    // ✅ Cache ALL expensive operations
    private readonly Lazy<Guid> _userId;
    private readonly Lazy<string> _userIdString;
    private readonly Lazy<string?> _userName;
    private readonly Lazy<string?> _email;
    private readonly Lazy<List<Claim>> _claims;
    private readonly Lazy<List<string>> _roles;
    private readonly Lazy<bool> _isAuthenticated;

    public CurrentUserSession(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
        
        _userId = new Lazy<Guid>(() =>
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            return Guid.TryParse(userIdClaim, out var guid) ? guid : Guid.Empty;
        });
        
        _userIdString = new Lazy<string>(() => 
            _userId.Value == Guid.Empty ? string.Empty : _userId.Value.ToString());
        
        _userName = new Lazy<string?>(() => 
            _httpContextAccessor.HttpContext?.User?.Identity?.Name);
        
        _email = new Lazy<string?>(() => 
            _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value);
        
        _claims = new Lazy<List<Claim>>(() => 
            _httpContextAccessor.HttpContext?.User?.Claims?.ToList() 
            ?? new List<Claim>());
        
        _roles = new Lazy<List<string>>(() =>
            _claims.Value
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList());
        
        _isAuthenticated = new Lazy<bool>(() => 
            _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false);
    }

    // ✅ All properties now use cached Lazy values
    public Guid UserId => _userId.Value;
    public string UserIdString => _userIdString.Value;
    public string? UserName => _userName.Value;
    public string? Email => _email.Value;
    public IEnumerable<Claim> Claims => _claims.Value;
    public IReadOnlyList<string> Roles => _roles.Value.AsReadOnly();
    public bool IsAuthenticated => _isAuthenticated.Value;
    
    // Derived properties
    public bool IsAdmin => _roles.Value.Contains("SystemAdmin");
    public bool IsContentAdmin => 
        _roles.Value.Contains("ContentAdmin") || IsAdmin;
}
