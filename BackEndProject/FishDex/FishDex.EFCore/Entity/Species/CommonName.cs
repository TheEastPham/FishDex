using System.ComponentModel.DataAnnotations;

namespace FishDex.EFCore.Entity.Species;

public class CommonName
{
    [Key]
    public int AutoCtr { get; set; }
    public int SpecCode { get; set; }
    public int? StockCode { get; set; }
    public string ComName { get; set; } = string.Empty;
    public string? Transliteration { get; set; }
    public string? CountryCode { get; set; }
    public string? Language { get; set; }
    public string? NameType { get; set; }
    public bool IsPreferred { get; set; }
    public int Rank { get; set; }
    public string? Remarks { get; set; }

    // ── Community contribution + moderation ───────────────────────
    // ContributedBy = null → tên gốc từ FishBase. Có giá trị → tên do user đóng góp.
    // IsVerified default true (mọi row FishBase = verified); tên user submit = false đến khi admin duyệt.
    public Guid? ContributedBy { get; set; }
    public bool IsVerified { get; set; } = true;
    public Guid? ReviewedBy { get; set; }
    public string? RejectionReason { get; set; }

    public virtual Species Species { get; set; } = null!;
}
