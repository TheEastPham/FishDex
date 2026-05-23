using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FishDex.EFCore.Entity.Stocks;

[Table("StockExternalRefs")]
public class StockExternalRef
{
    [Key, ForeignKey("Stock")]
    public int StockCode { get; set; }

    [MaxLength(50)] public string? GenBankID { get; set; }
    [MaxLength(50)] public string? RfeID { get; set; }
    [MaxLength(50)] public string? FIGIS_ID { get; set; }
    [MaxLength(50)] public string? EcotoxID { get; set; }
    [MaxLength(50)] public string? GMAD_ID { get; set; }
    [MaxLength(50)] public string? SAUP_ID { get; set; }
    [MaxLength(50)] public string? SAUP_Group { get; set; }
    [MaxLength(255)] public string? SAUP { get; set; }
    [MaxLength(50)] public string? BOLD_ID { get; set; }
    [MaxLength(50)] public string? MitoRef { get; set; }
    [MaxLength(255)] public string? AusMuseum { get; set; }
    [MaxLength(255)] public string? FishTrace { get; set; }
    [MaxLength(50)] public string? IGFAName { get; set; }
    [MaxLength(50)] public string? EssayID { get; set; }
    [MaxLength(50)] public string? ICESStockID { get; set; }
    [MaxLength(50)] public string? OsteoBaseID { get; set; }
    [MaxLength(50)] public string? DORIS_ID { get; set; }
    [MaxLength(50)] public string? FishipediaID { get; set; }
    [MaxLength(50)] public string? SocotraAtlasID { get; set; }
    [MaxLength(50)] public string? AFORO_ID { get; set; }
    [MaxLength(50)] public string? FishSounds_ID { get; set; }
    [MaxLength(50)] public string? StocksRefNo { get; set; }

    public virtual Stock Stock { get; set; }
}
