using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;
using System.Security.Claims;
using UserManagement.EFCore.Entities.User;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace UserManagement.API.Controllers.Connect;

[ApiExplorerSettings(IgnoreApi = true)]
[EnableCors("AllowOAuth")]
public class AuthorizationController(
    UserManager<UserEntity> userManager,
    SignInManager<UserEntity> signInManager) : ControllerBase
{
    [HttpGet("~/connect/authorize"), HttpPost("~/connect/authorize")]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenIddict server request cannot be retrieved.");

        var result = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);

        if (!result.Succeeded)
        {
            var redirectUri = Request.PathBase + Request.Path + QueryString.Create(
                Request.HasFormContentType ? [.. Request.Form] : [.. Request.Query]);

            return Challenge(
                authenticationSchemes: IdentityConstants.ApplicationScheme,
                properties: new AuthenticationProperties { RedirectUri = redirectUri });
        }

        var user = await userManager.GetUserAsync(result.Principal)
            ?? throw new InvalidOperationException("The user details cannot be retrieved.");

        var identity = new ClaimsIdentity(
            authenticationType: TokenValidationParameters.DefaultAuthenticationType,
            nameType: Claims.Name,
            roleType: Claims.Role);

        identity.SetClaim(Claims.Subject, user.Id.ToString())
                .SetClaim(Claims.Email, user.Email)
                .SetClaim(Claims.Name, user.UserName)
                .SetClaims(Claims.Role, [.. (await userManager.GetRolesAsync(user))]);

        identity.SetScopes(request.GetScopes());
        identity.SetDestinations(GetDestinations);

        return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpGet("~/connect/login")]
    [AllowAnonymous]
    public IActionResult Login([FromQuery] string returnUrl = "/")
    {
        var html = $$"""
            <!DOCTYPE html>
            <html>
            <head><title>Sign in – FishLover</title>
            <style>
              body { font-family:sans-serif; display:flex; justify-content:center; align-items:center; min-height:100vh; margin:0; background:#f0f2f5; }
              .box { background:#fff; padding:2rem; border-radius:8px; box-shadow:0 2px 8px rgba(0,0,0,.15); width:320px; }
              h2 { margin-top:0; color:#1a1a2e; }
              label { display:block; margin-bottom:.25rem; font-size:.875rem; color:#555; }
              input { width:100%; padding:.5rem; margin-bottom:1rem; border:1px solid #ccc; border-radius:4px; box-sizing:border-box; }
              button { width:100%; padding:.6rem; background:#0d6efd; color:#fff; border:none; border-radius:4px; cursor:pointer; font-size:1rem; }
              button:hover { background:#0b5ed7; }
            </style>
            </head>
            <body>
              <div class="box">
                <h2>FishLover Sign In</h2>
                <form method="post" action="/connect/login?returnUrl={{Uri.EscapeDataString(returnUrl)}}">
                  <label>Email</label>
                  <input type="email" name="email" required autofocus />
                  <label>Password</label>
                  <input type="password" name="password" required />
                  <button type="submit">Sign In</button>
                </form>
              </div>
            </body>
            </html>
            """;

        return Content(html, "text/html");
    }

    [HttpPost("~/connect/login")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginPost([FromQuery] string returnUrl = "/")
    {
        var email = Request.Form["email"].ToString();
        var password = Request.Form["password"].ToString();

        var user = await userManager.FindByEmailAsync(email);
        if (user is not null)
        {
            var signInResult = await signInManager.PasswordSignInAsync(user, password, isPersistent: false, lockoutOnFailure: true);
            if (signInResult.Succeeded)
                return LocalRedirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
        }

        var html = $$"""
            <!DOCTYPE html>
            <html>
            <head><title>Sign in – FishLover</title>
            <style>
              body { font-family:sans-serif; display:flex; justify-content:center; align-items:center; min-height:100vh; margin:0; background:#f0f2f5; }
              .box { background:#fff; padding:2rem; border-radius:8px; box-shadow:0 2px 8px rgba(0,0,0,.15); width:320px; }
              h2 { margin-top:0; color:#1a1a2e; }
              label { display:block; margin-bottom:.25rem; font-size:.875rem; color:#555; }
              input { width:100%; padding:.5rem; margin-bottom:1rem; border:1px solid #ccc; border-radius:4px; box-sizing:border-box; }
              button { width:100%; padding:.6rem; background:#0d6efd; color:#fff; border:none; border-radius:4px; cursor:pointer; font-size:1rem; }
              .error { color:#dc3545; font-size:.875rem; margin-bottom:.75rem; }
            </style>
            </head>
            <body>
              <div class="box">
                <h2>FishLover Sign In</h2>
                <p class="error">Invalid email or password.</p>
                <form method="post" action="/connect/login?returnUrl={{Uri.EscapeDataString(returnUrl)}}">
                  <label>Email</label>
                  <input type="email" name="email" required autofocus />
                  <label>Password</label>
                  <input type="password" name="password" required />
                  <button type="submit">Sign In</button>
                </form>
              </div>
            </body>
            </html>
            """;

        return Content(html, "text/html");
    }

    [HttpPost("~/connect/token")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenIddict server request cannot be retrieved.");

        if (!request.IsAuthorizationCodeGrantType())
            throw new InvalidOperationException("The specified grant type is not supported.");

        var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        var userId = result.Principal?.GetClaim(Claims.Subject);
        var user = userId is not null ? await userManager.FindByIdAsync(userId) : null;

        if (user is null)
        {
            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The token is no longer valid."
                }));
        }

        var identity = new ClaimsIdentity(result.Principal!.Claims,
            authenticationType: TokenValidationParameters.DefaultAuthenticationType,
            nameType: Claims.Name,
            roleType: Claims.Role);

        identity.SetClaim(Claims.Subject, user.Id.ToString())
                .SetClaim(Claims.Email, user.Email)
                .SetClaim(Claims.Name, user.UserName)
                .SetClaims(Claims.Role, [.. (await userManager.GetRolesAsync(user))]);

        identity.SetDestinations(GetDestinations);

        return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpGet("~/connect/userinfo"), HttpPost("~/connect/userinfo")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<IActionResult> UserInfo()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return Challenge(authenticationSchemes: OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);

        var claims = new Dictionary<string, object>
        {
            [Claims.Subject] = user.Id.ToString(),
            [Claims.Email] = user.Email ?? string.Empty,
            [Claims.Name] = user.UserName ?? string.Empty,
            [Claims.Role] = await userManager.GetRolesAsync(user)
        };

        return Ok(claims);
    }

    [HttpGet("~/connect/logout"), HttpPost("~/connect/logout")]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return SignOut(
            authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            properties: new AuthenticationProperties { RedirectUri = "/" });
    }

    private static IEnumerable<string> GetDestinations(Claim claim)
    {
        switch (claim.Type)
        {
            case Claims.Name:
            case Claims.Subject:
                yield return Destinations.AccessToken;
                yield return Destinations.IdentityToken;
                yield break;

            case Claims.Email:
                if (claim.Subject?.HasScope(Scopes.Email) is true)
                {
                    yield return Destinations.AccessToken;
                    yield return Destinations.IdentityToken;
                }
                yield break;

            case Claims.Role:
                if (claim.Subject?.HasScope(Scopes.Roles) is true)
                {
                    yield return Destinations.AccessToken;
                    yield return Destinations.IdentityToken;
                }
                yield break;

            default:
                yield return Destinations.AccessToken;
                yield break;
        }
    }
}
