namespace FishDex.Domain.DTOs.Species;

/// <summary>User đóng góp 1 tên địa phương cho loài FishBase đã tồn tại.</summary>
public record SubmitCommonNameRequest(
    string ComName,
    string Language = "Vietnamese",
    string? Transliteration = null);

public record RejectCommonNameRequest(string Reason);

/// <summary>Bản gọn cho "tên tôi đã gửi" + trang admin duyệt.</summary>
public record CommunityCommonNameDto(
    int AutoCtr,
    int SpecCode,
    string ComName,
    string? Language,
    bool IsVerified,
    string? RejectionReason,
    Guid? ContributedBy);
