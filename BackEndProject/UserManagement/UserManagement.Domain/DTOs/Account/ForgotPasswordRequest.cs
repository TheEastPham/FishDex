using System.ComponentModel.DataAnnotations;

namespace UserManagement.Domain.DTOs.Account;

public record ForgotPasswordRequest(
    [Required, EmailAddress] string Email
);
