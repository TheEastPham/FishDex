using System.ComponentModel.DataAnnotations;

namespace UserManagement.Domain.DTOs.Push;

public record SaveSubscriptionRequest(
    [Required] string Endpoint,
    [Required] string P256dh,
    [Required] string Auth,
    string? UserAgent
);
