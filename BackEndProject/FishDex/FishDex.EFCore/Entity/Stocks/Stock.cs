using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FishDex.EFCore.Entity.Stocks;

[Table("Stock")]
public class Stock
{
    [Key]
    public int StockCode { get; set; }
    public int SpecCode { get; set; }
    [MaxLength(255)] public string? SynOC { get; set; }
    public string? StockDefs { get; set; }
    public string? StockDefsGeneral { get; set; }
    [MaxLength(50)] public string? Level { get; set; }
    public bool LocalUnique { get; set; }

    public virtual StockConservation? Conservation { get; set; }
    public virtual StockEnvironment? Environment { get; set; }
    public virtual StockExternalRef? ExternalRefs { get; set; }
    public virtual StockDataAvailability? DataFlags { get; set; }
    public virtual StockMetadata? Metadata { get; set; }
}
