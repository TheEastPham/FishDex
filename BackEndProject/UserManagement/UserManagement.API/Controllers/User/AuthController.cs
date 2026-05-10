using FishLover.Shared.Services;
using UserManagement.Domain.DTOs.Auth;
using UserManagement.Domain.DTOs.Account;
using UserManagement.Domain.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UserManagement.API.Controllers.User;

[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
public class AuthController(IAuthService authService, ICurrentUserSession currentUser, ILogger<AuthController> logger)
    : ControllerBase
{
    /// <summary>
    /// User login
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await authService.LoginAsync(request);
            
            if (!response.Success)
            {
                return Unauthorized(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during login for user {Email}", request.Email);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Refresh access token
    /// </summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<TokenResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await authService.RefreshTokenAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error refreshing token");
            return Unauthorized("Invalid refresh token");
        }
    }

    /// <summary>
    /// User logout
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult> Logout()
    {
        try
        {
            var success = await authService.LogoutAsync(currentUser.UserId);
            if (success)
            {
                return Ok(new { message = "Logged out successfully" });
            }

            return BadRequest("Logout failed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during logout");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// User registration
    /// </summary>
    [HttpGet("valid-email/{email}?invitationCode={invitationCode}")]
    public async Task<ActionResult<EmailVerificationResponse>> GetVerificationCode(string email, string? invitationCode)
    {
        var response = await authService.GetVerificationCode(email, invitationCode);
            
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// User registration
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await authService.RegisterAsync(request);
            
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during registration for user {Email}", request.Email);
            return StatusCode(500, "Internal server error");
        }
    }
}
