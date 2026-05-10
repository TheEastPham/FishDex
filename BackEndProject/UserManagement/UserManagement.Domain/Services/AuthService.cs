using AutoMapper;
using UserManagement.Domain.DTOs.Auth;
using UserManagement.Domain.DTOs.Account;
using UserManagement.Domain.DTOs.User;
using UserManagement.Domain.Models;
using UserManagement.Domain.Services.Interfaces;
using UserManagement.EFCore.Entities.User;
using UserManagement.EFCore.Repositories.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using UserManagement.Domain.Helper;
using UserManagement.EFCore.Entities.Invitation;

namespace UserManagement.Domain.Services;

public class AuthService(
    UserManager<UserEntity> userManager,
    SignInManager<UserEntity> signInManager,
    IConfiguration configuration,
    IMapper mapper,
    ILogger<AuthService> logger,
    IEmailService emailService,
    IInvitationRepository invitationRepository,
    IInvitationUsedRepository invitationUsedRepository,
    IMemoryCache cache)
    : IAuthService
{
    private const int CodeExpiryMinutes = 5;


    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        try
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user == null || !user.IsActive)
            {
                return new LoginResponse(false, "Invalid email or password");
            }

            // Check if email is confirmed
            if (!user.EmailConfirmed)
            {
                return new LoginResponse(false, "Email not verified. Please check your email and verify your account.");
            }

            var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            if (!result.Succeeded)
            {
                return new LoginResponse(false, "Invalid email or password");
            }

            var roles = await userManager.GetRolesAsync(user);
            var tokens = await GenerateTokensAsync(user, roles);

            // Update refresh token in database
            user.RefreshToken = tokens.RefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            user.LastLoginAt = DateTime.UtcNow;
            await userManager.UpdateAsync(user);

            var userModel = mapper.Map<User>(user);
            userModel.Roles = roles.ToList();
            var userDto = mapper.Map<UserDto>(userModel);

            return new LoginResponse(
                true,
                "Login successful",
                tokens.AccessToken,
                tokens.RefreshToken,
                tokens.ExpiresAt,
                userDto
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during login for user {Email}", request.Email);
            return new LoginResponse(false, "An error occurred during login");
        }
    }

    public async Task<TokenResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        try
        {
            var user = await userManager.Users
                .FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken);

            if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                throw new SecurityTokenException("Invalid refresh token");
            }

            var roles = await userManager.GetRolesAsync(user);
            var tokens = await GenerateTokensAsync(user, roles);

            // Update refresh token
            user.RefreshToken = tokens.RefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await userManager.UpdateAsync(user);

            return tokens;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error refreshing token");
            throw;
        }
    }

    public async Task<bool> LogoutAsync(Guid userId)
    {
        try
        {
            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user == null) return false;

            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            var result = await userManager.UpdateAsync(user);

            return result.Succeeded;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during logout for user {UserId}", userId);
            return false;
        }
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        try
        {
            if (request.Password != request.ConfirmPassword)
            {
                return new RegisterResponse(false, "Passwords do not match");
            }

            var existingUser = await userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return new RegisterResponse(false, "Email already exists");
            }

            var cacheKey = GetCacheKey(request.Email);
            if (!cache.TryGetValue(cacheKey, out VerificationCode? cacheData))
            {
                return new RegisterResponse(false, "Verification code expired or not found");
            }

            if (cacheData?.Code != request.VerificationCode)
            {
                return new RegisterResponse(false, "Invalid verification code");
            }
            
            var user = await CreateUserAsync(request);
            if (user == null)
            {
                return new RegisterResponse(false, "Failed to create user");
            }

            await emailService.SendWelcomeEmailAsync(user.Email!, user.FirstName!);
            if (cacheData.InvitationId.HasValue)
            {
                await MarkInvitationAsUsedAsync(cacheData.InvitationId.Value, user.Id);
            }
            return new RegisterResponse(true,
                "Registration successful. Please check your email to verify your account.",
                user.Id.ToString(), true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during registration for user {Email}", request.Email);
            return new RegisterResponse(false, "An error occurred during registration");
        }
    }

    private Task<TokenResponse> GenerateTokensAsync(UserEntity user, IList<string> roles)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];
        var expiryInDays = int.Parse(jwtSettings["ExpiryInDays"] ?? "7");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var expiresAt = DateTime.UtcNow.AddDays(expiryInDays);
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = Guid.NewGuid().ToString();

        return Task.FromResult(new TokenResponse(accessToken, refreshToken, expiresAt));
    }
    
    public async Task<EmailVerificationResponse> GetVerificationCode(string email, string? invitationCode)
    {
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            return new EmailVerificationResponse(false, "Email already exists");
        }

        var code = GeneratorCode.GenerateRandomCode(email, 8);
        var cacheKey = GetCacheKey(email);
        var cacheData = new VerificationCode
        {
            Code = code,
            CreatedAt = DateTime.UtcNow
        };

        if (configuration.GetValue<bool>("RequireInvitation"))
        {
            var validation = await ValidateInvitationCodeAsync(invitationCode);
            cacheData.InvitationId = validation.InvitationId;
            
            if(!validation.IsValid)
                return new EmailVerificationResponse(false, validation.Message);
        }
        
        await emailService.SendVerificationCodeAsync(email, code);
        
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(CodeExpiryMinutes));
            
        cache.Set(cacheKey, cacheData, cacheOptions);
        
        return new EmailVerificationResponse(true, string.Empty);
    }

     private async Task<UserEntity?> CreateUserAsync(RegisterRequest request)
    {
        var user = new UserEntity
        {
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Language = request.Language,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true,
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            logger.LogError("Failed to create user: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
            return null;
        }

        await userManager.AddToRoleAsync(user, "Member");
        return user;
    }

    private async Task<ValidateInvitationResponse> ValidateInvitationCodeAsync(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return new ValidateInvitationResponse
            {
                IsValid = false,
                Message = "Invitation code is required"
            };
        }

        var invitation = await invitationRepository.GetByCodeAsync(code);
        if (invitation == null)
        {
            return new ValidateInvitationResponse
            {
                IsValid = false,
                Message = "Invalid invitation code"
            };
        }

        if (invitation.ExpiryDate < DateTime.UtcNow)
        {
            return new ValidateInvitationResponse
            {
                IsValid = false,
                Message = "Invitation code has expired"
            };
        }

        var usageCount = invitation.UsedBy.Count;
        if (usageCount >= invitation.MaxUses)
        {
            return new ValidateInvitationResponse
            {
                IsValid = false,
                Message = "Invitation code has reached maximum usage limit"
            };
        }

        return new ValidateInvitationResponse
        {
            IsValid = true,
            Message = string.Empty,
            InvitationId = invitation.Id
        };
    }

    private async Task MarkInvitationAsUsedAsync(Guid invitationId, Guid userId)
    {
        var invitationUsed = new InvitationUsed
        {
            InvitationId = invitationId,
            UserId = userId,
            UsedDate = DateTime.UtcNow
        };

        await invitationUsedRepository.CreateAsync(invitationUsed);
    }
    
    private static string GetCacheKey(string email) => $"verification_code:{email.ToLowerInvariant()}";

}