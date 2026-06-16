using FishLover.Shared.Services;
using UserManagement.Domain.DTOs.Auth;
using UserManagement.Domain.DTOs.Account;
using UserManagement.Domain.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

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
    [HttpGet("valid-email/{email}")]
    public async Task<ActionResult<EmailVerificationResponse>> GetVerificationCode(string email, [FromQuery] string? invitationCode)
    {
        var response = await authService.GetVerificationCode(email, invitationCode);
            
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Forgot password — sends reset link to email
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var sent = await authService.ForgotPasswordAsync(request.Email);
        if (!sent)
            return StatusCode(500, new { message = "Failed to send reset email. Please try again later." });

        // Always same message — don't leak whether email exists
        return Ok(new { message = "If this email exists, a reset link has been sent." });
    }

    /// <summary>
    /// Reset password with token from email link
    /// </summary>
    [HttpPost("reset-password")]
    public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var success = await authService.ResetPasswordAsync(request);
        if (!success)
            return BadRequest(new { message = "Invalid or expired reset token." });

        return Ok(new { message = "Password reset successfully." });
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
