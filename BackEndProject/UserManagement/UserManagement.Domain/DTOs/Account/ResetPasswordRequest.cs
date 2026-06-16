using System.ComponentModel.DataAnnotations;

namespace UserManagement.Domain.DTOs.Account;

public record ResetPasswordRequest(
    [Required, EmailAddress] string Email,
    [Required] string Token,
    [Required, MinLength(8)] string NewPassword,
    [Required] string ConfirmPassword
);
