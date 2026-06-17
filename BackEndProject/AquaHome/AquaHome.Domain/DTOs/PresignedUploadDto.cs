namespace AquaHome.Domain.DTOs;

/// <summary>Trả về FE để upload thẳng lên MinIO.</summary>
public record PresignedUploadDto(
    Guid   MediaId,
    string UploadUrl,   // presigned PUT URL, expire sau PresignedPutUrlExpiryMinutes
    string ObjectKey
);
