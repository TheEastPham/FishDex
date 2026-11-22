using System.Globalization;

namespace FishDex.Domain.Storage;

/// <summary>
/// Key policy của bucket FishDex (system-image). Mọi key do FishDex sinh ra đi qua đây — trừ ảnh FishBase,
/// key của chúng là <c>SystemImage.ObjectKey</c> (<c>{specCode}/{id}{ext}</c>).
/// </summary>
/// <remarks>Key đã lưu trong DB (<c>SpeciesSnapshot.ThumbnailObjectKey</c>) — đổi format là ảnh cũ mất. Chỉ thêm, không sửa.</remarks>
public static class FishDexObjectKeys
{
    /// <summary>Tiền tố ảnh do user upload cho loài cộng đồng — phải qua kiểm duyệt mới công khai.</summary>
    public const string CommunityPrefix = "community/";

    /// <summary>Trần dung lượng ảnh bìa loài cộng đồng. Presigned PUT không tự chặn được — BE HEAD ở bước confirm và lúc redeem.</summary>
    public const long CommunityCoverMaxBytes = 1 * 1024 * 1024;

    /// <summary><c>community/{specCode}/</c> — mọi object của một loài cộng đồng (có dấu '/' cuối để 50000 không khớp 500001).</summary>
    public static string CommunityFolder(int specCode)
    {
        return $"{CommunityPrefix}{specCode.ToString(CultureInfo.InvariantCulture)}/";
    }

    /// <summary><c>community/{specCode}/cover{ext}</c> — <paramref name="extension"/> gồm dấu chấm, vd ".jpg".</summary>
    public static string CommunityCover(int specCode, string extension)
    {
        return $"{CommunityFolder(specCode)}cover{extension}";
    }

    /// <summary>Key có thuộc vùng ảnh cộng đồng không; nếu có thì lấy SpecCode từ segment thứ hai.</summary>
    public static bool TryParseCommunity(string objectKey, out int specCode)
    {
        specCode = 0;
        if (!objectKey.StartsWith(CommunityPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var segments = objectKey.Split('/');
        return segments.Length >= 2 && int.TryParse(segments[1], NumberStyles.None, CultureInfo.InvariantCulture, out specCode);
    }
}
