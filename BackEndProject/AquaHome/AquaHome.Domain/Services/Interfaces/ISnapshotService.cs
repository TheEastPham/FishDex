using AquaHome.Domain.DTOs;
using FishLover.Shared.Common;

namespace AquaHome.Domain.Services.Interfaces;

public interface ISnapshotService
{
    Task<SnapshotPreviewDto?> PreviewAsync(Guid aquariumId, CancellationToken ct = default);
    Task<AquariumSnapshotDto?> PublishAsync(Guid aquariumId, PublishSnapshotRequest request, CancellationToken ct = default);
    Task<bool> UnpublishAsync(Guid snapshotId, CancellationToken ct = default);

    Task<PagedResult<AquariumSnapshotDto>> GetGalleryAsync(
        int? waterType, int? style, string? contest, string sort, int page, int pageSize, CancellationToken ct = default);
    Task<AquariumSnapshotDto?> GetBySlugAsync(string slug, CancellationToken ct = default);

    Task<bool> LikeAsync(Guid snapshotId, CancellationToken ct = default);
    Task<bool> UnlikeAsync(Guid snapshotId, CancellationToken ct = default);
}
