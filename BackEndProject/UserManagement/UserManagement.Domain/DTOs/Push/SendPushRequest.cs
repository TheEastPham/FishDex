using System.ComponentModel.DataAnnotations;

namespace UserManagement.Domain.DTOs.Push;

public record SendPushRequest(
    [Required] Guid UserId,
    [Required] string Title,
    [Required] string Body,
    string? Url
);
