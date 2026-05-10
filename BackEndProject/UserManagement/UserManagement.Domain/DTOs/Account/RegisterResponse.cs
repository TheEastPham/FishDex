namespace UserManagement.Domain.DTOs.Account;

public record RegisterResponse(
    bool Success,
    string Message,
    string? UserId = null,
    bool RequiresEmailVerification = true
);


public record EmailVerificationResponse(
    bool Success,
    string Message
);