using FishDex.EFCore.Entity.Ecologies;
using FishDex.EFCore.Entity.Media;
using FishDex.EFCore.Entity.Species;

namespace FishDex.Domain.SpeciesProfiles;

/// <summary>
/// Các rule đọc dữ liệu FishBase — MỘT bản duy nhất cho detail, search, related, summaries,
/// market và ảnh đại diện. Trước đây mỗi đường đọc tự viết rule riêng và đã lệch nhau
/// (chọn dòng ecology, ảnh đực/cái, thumbnail fallback, tên thường gọi). Đổi rule ở đây là
/// đổi ở mọi màn hình cùng lúc.
/// </summary>
internal static class SpeciesRules
{
    public static string? NormalizeLanguage(string? lang)
    {
        return lang?.ToLowerInvariant() switch
        {
            "vn" or "vi" or "vietnamese" => "Vietnamese",
            "en" or "eng" or "english"   => "English",
            null                          => null,
            var other                     => other
        };
    }

    /// <summary>
    /// Tên thường gọi: ngôn ngữ yêu cầu → English. Trong mỗi ngôn ngữ: tên IsPreferred trước,
    /// rồi Rank nhỏ trước — tên do cộng đồng đóng góp (Rank 999) chỉ thắng khi FishBase không có.
    /// <paramref name="verifiedNames"/> phải đã lọc IsVerified.
    /// </summary>
    public static string? PickCommonName(IEnumerable<CommonName> verifiedNames, string? language)
    {
        var names = verifiedNames as IReadOnlyCollection<CommonName> ?? verifiedNames.ToList();

        if (language != null)
        {
            var inLang = BestName(names.Where(c => c.Language == language));
            if (inLang != null) return inLang;
        }

        return BestName(names.Where(c => c.Language == "English"));
    }

    private static string? BestName(IEnumerable<CommonName> pool)
    {
        return pool
            .OrderByDescending(c => c.IsPreferred)
            .ThenBy(c => c.Rank)
            .FirstOrDefault()?.ComName;
    }

    /// <summary>
    /// Một loài thường có nhiều dòng Ecology (mỗi Stock một dòng), và DB còn sót các dòng
    /// rác từ bản ETL cũ — bản đó không truyền EcologyId nên để serial tự sinh. Sub-table
    /// (FeedingAndDiet/Associations/HabitatZone/...) khoá theo autoctr, nên những dòng rác
    /// đó không có sub-row nào. ETL hiện tại luôn ghi EcologyId = autoctr — đó là dấu hiệu
    /// nhận ra dòng thật. Thiếu thứ tự thì FirstOrDefault() chọn ngẫu nhiên, vớ phải dòng
    /// rác là cả khối Sinh thái học của loài biến mất dù dữ liệu vẫn nằm trong DB.
    /// </summary>
    public static Ecology? PickEcology(IEnumerable<Ecology> rows)
    {
        return rows
            .OrderByDescending(e => e.EcologyId == e.autoctr)
            .ThenBy(e => e.EcologyId)
            .FirstOrDefault();
    }

    /// <summary>
    /// Ảnh đại diện (thumbnail): PicPreferred → ảnh đực ưu tiên → ảnh cái ưu tiên → ảnh bất kỳ
    /// (Id nhỏ nhất, để kết quả ổn định giữa các lần gọi).
    /// </summary>
    public static SystemImage? PickThumbnail(IEnumerable<SystemImage> images)
    {
        return images
            .OrderByDescending(i => i.PicPreferred == true)
            .ThenByDescending(i => i.PicPreferredMale == true)
            .ThenByDescending(i => i.PicPreferredFem == true)
            .ThenBy(i => i.Id)
            .FirstOrDefault();
    }

    /// <summary>
    /// Ảnh đực/cái: cờ trên ảnh (PicPreferredMale/Fem) trước; không ảnh nào có cờ thì khớp tên
    /// file với Species.PicPreferredNameM/F. Không có thì null — không đoán.
    /// </summary>
    public static SpeciesImages PickImages(IReadOnlyCollection<SystemImage> images, string? picNameMale, string? picNameFemale)
    {
        var male = images.FirstOrDefault(i => i.PicPreferredMale == true)
                   ?? (picNameMale is null ? null : images.FirstOrDefault(i => i.Name == picNameMale));
        var female = images.FirstOrDefault(i => i.PicPreferredFem == true)
                     ?? (picNameFemale is null ? null : images.FirstOrDefault(i => i.Name == picNameFemale));

        return new SpeciesImages(PickThumbnail(images), male, female);
    }

    /// <summary>
    /// FishBase để trống DietTroph nhiều hơn hẳn FoodTroph (126 vs 1.162 dòng trên bộ hiện
    /// tại) và bản thân nó cũng lấy FoodTroph khi thiếu DietTroph. Ngoài ra bản ETL cũ ghi 0
    /// thay vì NULL cho các dòng không có số liệu, nên phải coi 0 là "không có" — dữ liệu 0
    /// đó vẫn còn trong DB tới khi ETL chạy lại.
    /// </summary>
    public static decimal? PickTrophicLevel(FeedingAndDiet? feeding)
    {
        return Meaningful(feeding?.DietTroph) ?? Meaningful(feeding?.FoodTroph);
    }

    private static decimal? Meaningful(decimal? value)
    {
        return value is > 0 ? value : null;
    }

    public static IReadOnlyList<string> HabitatZones(HabitatZone? hz)
    {
        if (hz == null) return [];
        var zones = new List<string>();
        if (hz.Neritic)   zones.Add("Neritic");
        if (hz.Estuaries) zones.Add("Estuaries");
        if (hz.Mangroves) zones.Add("Mangroves");
        if (hz.Stream)    zones.Add("Stream");
        if (hz.Lakes)     zones.Add("Lakes");
        return zones;
    }
}

internal sealed record SpeciesImages(SystemImage? Thumbnail, SystemImage? Male, SystemImage? Female);
