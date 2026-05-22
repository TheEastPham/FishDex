using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FishDex.EFCore.Entity.Ecosystem;

/// <summary>
/// ecosystem.csv — junction table: loài nào xuất hiện ở hệ sinh thái nào
/// </summary>
[Table("Ecosystem")]
public class Ecosystem
{
    [Key]
    public int AutoCtr { get; set; }

    public int E_CODE { get; set; }
    public int SpecCode { get; set; }
    public int? StockCode { get; set; }

    [MaxLength(50)]
    public string? Status { get; set; }             // native, introduced, ...

    [MaxLength(50)]
    public string? CurrentPresence { get; set; }    // present, absent, ...

    [MaxLength(50)]
    public string? Abundance { get; set; }          // common, rare, ...

    [MaxLength(50)]
    public string? LifeStage { get; set; }

    public string? Remarks { get; set; }

    public int? EcosystemRefNo { get; set; }

    // ─── Audit ────────────────────────────────────────────────────
    public string? Entered { get; set; }
    public DateTime? Dateentered { get; set; }
    public string? Modified { get; set; }
    public DateTime? Datemodified { get; set; }
    public DateTime? TS { get; set; }

    // ─── Navigation ───────────────────────────────────────────────
    [ForeignKey(nameof(E_CODE))]
    public virtual EcosystemRef? EcosystemRef { get; set; }
}
