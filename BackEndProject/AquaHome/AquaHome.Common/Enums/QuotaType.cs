namespace AquaHome.Common.Enums;

public enum QuotaType
{
    MaxFavorites = 1,
    MaxAquariums = 2,
    SearchPerDay = 3,
    AiQaPerDay = 4,
    ImageSearchPerDay = 5,

    /// <summary>
    /// Không phải quota theo role: lượt upload video contest lên YouTube trong 1 ngày (giờ Pacific).
    /// Row <c>UserId = user</c> là lượt của user đó; <c>UserId = Guid.Empty</c> là tổng cả hệ thống.
    /// </summary>
    ContestYouTubeUploadPerDay = 6,
}
