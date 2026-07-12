namespace AquaHome.EFCore.Entity;

public class ContestSponsor
{
    public Guid Id { get; set; }
    public Guid ContestId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? WebsiteUrl { get; set; } // website hoặc Facebook Page — link chung, không phân biệt loại
    public string? Address { get; set; }

    /// <summary>R2 object key — resolve presigned URL mới mỗi lần đọc (không lưu URL cố định, tránh hết hạn).</summary>
    public string? LogoObjectKey { get; set; }

    public int SponsorTier { get; set; } // enum SponsorTier: Platinum/Gold/Silver/Bronze/Partner
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public Contest Contest { get; set; } = null!;
}
