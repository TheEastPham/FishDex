using UserManagement.Domain.DTOs.User;
using UserManagement.Domain.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using FishLover.Shared.Services;

namespace UserManagement.API.Controllers.User;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AccountController(
    IUserService userService,
    ICurrentUserSession currentUser,
    ILogger<AccountController> logger)
    : ControllerBase
{
    /// <summary>
    /// Get current user's profile
    /// </summary>
    [HttpGet("profile")]
    public async Task<ActionResult<UserDto>> GetProfile()
    {
        try
        {
            if (!currentUser.IsAuthenticated)
                return Unauthorized();

            var user = await userService.GetUserByIdAsync(currentUser.UserId);
            return Ok(user);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving current user profile");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update current user's profile
    /// </summary>
    [HttpPut("profile")]
    public async Task<ActionResult<UserDto>> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        try
        {
            if (!currentUser.IsAuthenticated)
                return Unauthorized();

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Map to UpdateUserRequest with current user ID
            var updateRequest = new UpdateUserRequest(
                Id: currentUser.UserId,
                FirstName: request.FirstName,
                LastName: request.LastName,
                PhoneNumber: request.PhoneNumber
            );

            var user = await userService.UpdateUserAsync(updateRequest);
            return Ok(user);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating current user profile");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Change current user's password
    /// </summary>
    [HttpPut("password")]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            if (!currentUser.IsAuthenticated)
                return Unauthorized();

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var success = await userService.ChangePasswordAsync(currentUser.UserId, request.CurrentPassword, request.NewPassword);
            if (!success)
            {
                return BadRequest("Current password is incorrect");
            }

            return Ok(new { message = "Password changed successfully" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error changing password for user");
            return StatusCode(500, "Internal server error");
        }
    }
}
