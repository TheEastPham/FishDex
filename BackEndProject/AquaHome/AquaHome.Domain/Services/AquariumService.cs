using AquaHome.Domain.DTOs;
using AquaHome.Domain.Services.Interfaces;
using AquaHome.EFCore.Entity;
using AquaHome.EFCore.Repository.Interface;
using FishLover.Shared.Services;

namespace AquaHome.Domain.Services;

public class AquariumService(
    IAquariumRepository aquariumRepo,
    ICurrentUserSession currentUser) : IAquariumService
{
    public async Task<IReadOnlyList<AquariumDto>> GetMyAquariumsAsync(CancellationToken ct = default)
    {
        var list = await aquariumRepo.GetByUserAsync(currentUser.UserId, ct);
        return list.Select(ToDto).ToList();
    }

    public async Task<AquariumDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var aquarium = await aquariumRepo.GetByIdAndUserAsync(id, currentUser.UserId, ct);
        return aquarium is null ? null : ToDto(aquarium);
    }

    public async Task<AquariumDto> CreateAsync(CreateAquariumRequest request, CancellationToken ct = default)
    {
        var entity = new Aquarium
        {
            Id           = Guid.NewGuid(),
            UserId       = currentUser.UserId,
            Name         = request.Name,
            VolumeLiters = request.VolumeLiters,
            Type         = request.Type,
            Description  = request.Description,
            CreatedAt    = DateTime.UtcNow
        };

        await aquariumRepo.AddAsync(entity);
        return ToDto(entity);
    }

    public async Task<AquariumDto?> UpdateAsync(Guid id, UpdateAquariumRequest request, CancellationToken ct = default)
    {
        var entity = await aquariumRepo.GetByIdAndUserAsync(id, currentUser.UserId, ct);
        if (entity is null) return null;

        if (request.Name        is not null) entity.Name         = request.Name;
        if (request.VolumeLiters.HasValue)   entity.VolumeLiters = request.VolumeLiters;
        if (request.Type        is not null) entity.Type         = request.Type;
        if (request.Description is not null) entity.Description  = request.Description;

        await aquariumRepo.UpdateAsync(entity);
        return ToDto(entity);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await aquariumRepo.GetByIdAndUserAsync(id, currentUser.UserId, ct);
        if (entity is null) return false;

        await aquariumRepo.DeleteAsync(entity);
        return true;
    }

    private static AquariumDto ToDto(Aquarium a) => new(
        a.Id, a.Name, a.VolumeLiters, a.Type, a.Description, a.CreatedAt,
        a.Fish?.Count ?? 0);
}
