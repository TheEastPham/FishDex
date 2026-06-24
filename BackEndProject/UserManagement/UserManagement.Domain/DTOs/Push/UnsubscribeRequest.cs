using System.ComponentModel.DataAnnotations;

namespace UserManagement.Domain.DTOs.Push;

public record UnsubscribeRequest(
    [Required] string Endpoint
);
