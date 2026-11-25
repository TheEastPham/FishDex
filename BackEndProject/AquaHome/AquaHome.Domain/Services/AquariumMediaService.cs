using AquaHome.Domain.DTOs;
using AquaHome.Domain.Services.Interfaces;
using AquaHome.Domain.Storage;
using AquaHome.EFCore.Entity;
using AquaHome.EFCore.Repository.Interface;
using FishLover.Shared.Services;
using FishLover.Storage;
using Microsoft.Extensions.Logging;

namespace AquaHome.Domain.Services;

public class AquariumMediaService(
    IAquariumMediaRepository mediaRepo,
    IAquariumRepository      aquariumRepo,
    IStorageService          storage,
    UploadVerifier           uploadVerifier,
    ICurrentUserSession      currentUser,
    AquaHomeObjectKeys keys,
    ILogger<AquariumMediaService> logger) : IAquariumMediaService
{
    private const int MaxPhotosPerAquarium = 10;
    private const long MaxUploadBytes      = 3L * 1024 * 1024 / 2; // 1.5 MB — FE phải nén ảnh dưới mức này

    // Đuôi file suy từ content-type (đã ký vào presigned PUT), không lấy theo tên file client gửi.
    private static readonly IReadOnlyDictionary<string, string> ImageExtensions = UploadContentTypes.Images;


    public async Task<PresignedUploadDto?> RequestUploadAsync(
        Guid aquariumId, string fileName, string contentType, CancellationToken ct = default)
    {
        if (!ImageExtensions.TryGetValue(contentType, out var ext))
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
            // ObjectKey lấy đuôi từ FileName → ép đuôi theo content-type ngay đây; row cũ giữ nguyên key.
            FileName    = Path.GetFileNameWithoutExtension(fileName) + ext,
            ContentType = contentType,
            CreatedAt   = DateTime.UtcNow,
        };

        var objectKey  = keys.AquariumMedia(media);
        var uploadUrl  = await storage.GeneratePresignedPutUrlAsync(objectKey, contentType, ct);

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

        // Presigned PUT không chặn được dung lượng → kiểm file thật. Không có file hoặc quá 1.5MB thì bỏ hẳn ảnh
        // (row tạo sẵn lúc xin URL, để lại sẽ chiếm 1 trong 10 suất ảnh của bể).
        var objectKey = keys.AquariumMedia(media);
        var verdict = await uploadVerifier.VerifyAsync(objectKey, new UploadPolicy(MaxUploadBytes), ct);
        if (!verdict.IsOk)
        {
            // Quá cỡ thì verifier đã xoá object; còn lại là bỏ dòng DB.
            logger.LogWarning("Rejected aquarium photo {MediaId}: {Verdict}, R2 size {Size}", mediaId, verdict.Kind, verdict.Size);
            mediaRepo.Remove(media);
            await mediaRepo.SaveChangesAsync(ct);
            return null;
        }

        var url = await storage.GetPresignedUrlAsync(objectKey, ct);
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
            var url = await storage.GetPresignedUrlAsync(keys.AquariumMedia(m), ct);
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

        await storage.DeleteAsync(keys.AquariumMedia(media), ct);
        mediaRepo.Remove(media);
        await mediaRepo.SaveChangesAsync(ct);
        return true;
    }

    private static AquariumMediaDto ToDto(AquariumMedia m, string? url) =>
        new(m.Id, m.AquariumId, m.FileName, m.ContentType, m.CreatedAt, url);
}
