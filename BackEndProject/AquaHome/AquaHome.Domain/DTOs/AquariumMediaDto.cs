namespace AquaHome.Domain.DTOs;

public record AquariumMediaDto(
    Guid     Id,
    Guid     AquariumId,
    string   FileName,
    string   ContentType,
    DateTime CreatedAt,
    string?  Url          // presigned GET URL, null nếu storage chưa cấu hình
);
