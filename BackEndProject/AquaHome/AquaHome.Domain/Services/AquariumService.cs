using AquaHome.Domain.DTOs;
using AquaHome.Domain.Enums;
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
            Id          = Guid.NewGuid(),
            UserId      = currentUser.UserId,
            Name        = request.Name,
            LengthCm    = request.LengthCm,
            WidthCm     = request.WidthCm,
            HeightCm    = request.HeightCm,
            WaterType   = (int?)request.WaterType,
            Style       = (int?)request.Style,
            Description = request.Description,
            CreatedAt   = DateTime.UtcNow
        };

        await aquariumRepo.AddAsync(entity);
        return ToDto(entity);
    }

    public async Task<AquariumDto?> UpdateAsync(Guid id, UpdateAquariumRequest request, CancellationToken ct = default)
    {
        var entity = await aquariumRepo.GetByIdAndUserAsync(id, currentUser.UserId, ct);
        if (entity is null) return null;

        if (request.Name        is not null) entity.Name        = request.Name;
        if (request.LengthCm.HasValue)       entity.LengthCm    = request.LengthCm;
        if (request.WidthCm.HasValue)        entity.WidthCm     = request.WidthCm;
        if (request.HeightCm.HasValue)       entity.HeightCm    = request.HeightCm;
        if (request.WaterType.HasValue) entity.WaterType = (int)request.WaterType.Value;
        if (request.Style.HasValue)     entity.Style     = (int)request.Style.Value;
        if (request.Description is not null) entity.Description = request.Description;

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

    public async Task<bool> AddFishAsync(Guid aquariumId, int specCode, int quantity, CancellationToken ct = default)
    {
        var aquarium = await aquariumRepo.GetByIdAndUserAsync(aquariumId, currentUser.UserId, ct);
        if (aquarium is null) return false;

        var existing = await aquariumRepo.GetFishEntryAsync(aquariumId, specCode, ct);
        if (existing is not null)
        {
            existing.Quantity += quantity;
            await aquariumRepo.UpdateAsync(aquarium); // triggers SaveChanges
            return true;
        }

        await aquariumRepo.AddFishAsync(new AquariumFish
        {
            AquariumId = aquariumId,
            SpecCode   = specCode,
            Quantity   = quantity,
            AddedAt    = DateTime.UtcNow,
        }, ct);
        return true;
    }

    public async Task<bool> RemoveFishAsync(Guid aquariumId, int specCode, CancellationToken ct = default)
    {
        var aquarium = await aquariumRepo.GetByIdAndUserAsync(aquariumId, currentUser.UserId, ct);
        if (aquarium is null) return false;

        var entry = await aquariumRepo.GetFishEntryAsync(aquariumId, specCode, ct);
        if (entry is null) return false;

        await aquariumRepo.RemoveFishAsync(entry, ct);
        return true;
    }

    public async Task<IReadOnlyList<AquariumFishDto>?> GetFishListAsync(Guid aquariumId, CancellationToken ct = default)
    {
        // Ownership check: 1 PK lookup on Aquariums table
        var aquarium = await aquariumRepo.GetByIdAndUserAsync(aquariumId, currentUser.UserId, ct);
        if (aquarium is null) return null;

        // Fish list: 1 indexed query on AquariumFish WHERE AquariumId = @id; mapping done here, not in repo
        var entities = await aquariumRepo.GetFishListAsync(aquariumId, ct);
        return entities.Select(f => new AquariumFishDto(f.SpecCode, f.Quantity, f.AddedAt)).ToList();
    }

    private static AquariumDto ToDto(Aquarium a) => new(
        a.Id, a.Name, a.LengthCm, a.WidthCm, a.HeightCm, a.VolumeLiters,
        a.WaterType.HasValue ? (WaterType)a.WaterType.Value : null,
        a.Style.HasValue     ? (AquariumStyle)a.Style.Value : null,
        a.Description, a.CreatedAt, a.Fish?.Count ?? 0);
}
