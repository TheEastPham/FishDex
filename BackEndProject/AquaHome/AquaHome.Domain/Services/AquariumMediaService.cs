using AquaHome.Domain.DTOs;
using AquaHome.Domain.Services.Interfaces;
using AquaHome.Domain.Settings;
using AquaHome.EFCore.Entity;
using AquaHome.EFCore.Repository.Interface;
using FishLover.Shared.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AquaHome.Domain.Services;

public class AquariumMediaService(
    IAquariumMediaRepository mediaRepo,
    IAquariumRepository      aquariumRepo,
    IStorageService          storage,
    ICurrentUserSession      currentUser,
    IOptions<StorageSettings> storageOptions,
    ILogger<AquariumMediaService> logger) : IAquariumMediaService
{
    private const int MaxPhotosPerAquarium = 10;
    private const long MaxUploadBytes      = 1 * 1024 * 1024; // 1 MB

    private static readonly HashSet<string> AllowedContentTypes =
        ["image/jpeg", "image/png", "image/webp"];

    private string Env => storageOptions.Value.Environment;

    public async Task<PresignedUploadDto?> RequestUploadAsync(
        Guid aquariumId, string fileName, string contentType, CancellationToken ct = default)
    {
        if (!AllowedContentTypes.Contains(contentType))
        {
            logger.LogWarning("Rejected upload for {AquariumId}: unsupported content-type {CT}", aquariumId, contentType);
            return null;
        }

        // Verify ownership
        var aquarium = await aquariumRepo.GetByIdAndUserAsync(aquariumId, currentUser.UserId, ct);
        if (aquarium is null)
            return null;

        var count = await mediaRepo.CountByAquariumAsync(aquariumId, ct);
        if (count >= MaxPhotosPerAquarium)
        {
            logger.LogInformation("Aquarium {AquariumId} has reached {Max} photos limit", aquariumId, MaxPhotosPerAquarium);
            return null;
        }

        var media = new AquariumMedia
        {
            Id          = Guid.NewGuid(),
            AquariumId  = aquariumId,
            FileName    = fileName,
            ContentType = contentType,
            CreatedAt   = DateTime.UtcNow,
        };

        var objectKey  = media.ObjectKey(Env);
        var uploadUrl  = await storage.GeneratePresignedPutUrlAsync(objectKey, contentType, MaxUploadBytes, ct);

        if (uploadUrl is null)
            return null;

        await mediaRepo.AddAsync(media, ct);
        await mediaRepo.SaveChangesAsync(ct);

        return new PresignedUploadDto(media.Id, uploadUrl, objectKey);
    }

    public async Task<AquariumMediaDto?> ConfirmUploadAsync(
        Guid aquariumId, Guid mediaId, CancellationToken ct = default)
    {
        var media = await mediaRepo.GetByIdAsync(mediaId, ct);
        if (media is null || media.AquariumId != aquariumId)
            return null;

        // Verify ownership (aquarium still belongs to current user)
        var aquarium = await aquariumRepo.GetByIdAndUserAsync(aquariumId, currentUser.UserId, ct);
        if (aquarium is null)
            return null;

        var url = await storage.GetPresignedUrlAsync(media.ObjectKey(Env), ct);
        return ToDto(media, url);
    }

    public async Task<IReadOnlyList<AquariumMediaDto>> GetMediaAsync(
        Guid aquariumId, CancellationToken ct = default)
    {
        var aquarium = await aquariumRepo.GetByIdAndUserAsync(aquariumId, currentUser.UserId, ct);
        if (aquarium is null)
            return [];

        var list = await mediaRepo.GetByAquariumAsync(aquariumId, ct);

        var dtos = new List<AquariumMediaDto>(list.Count);
        foreach (var m in list)
        {
            var url = await storage.GetPresignedUrlAsync(m.ObjectKey(Env), ct);
            dtos.Add(ToDto(m, url));
        }
        return dtos;
    }

    public async Task<bool> DeleteAsync(Guid aquariumId, Guid mediaId, CancellationToken ct = default)
    {
        var aquarium = await aquariumRepo.GetByIdAndUserAsync(aquariumId, currentUser.UserId, ct);
        if (aquarium is null)
            return false;

        var media = await mediaRepo.GetByIdAsync(mediaId, ct);
        if (media is null || media.AquariumId != aquariumId)
            return false;

        await storage.DeleteAsync(media.ObjectKey(Env), ct);
        mediaRepo.Remove(media);
        await mediaRepo.SaveChangesAsync(ct);
        return true;
    }

    private static AquariumMediaDto ToDto(AquariumMedia m, string? url) =>
        new(m.Id, m.AquariumId, m.FileName, m.ContentType, m.CreatedAt, url);
}
