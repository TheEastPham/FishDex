using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// Bảng gốc: Chứa thông tin định danh cơ bản
namespace FishDex.EFCore.Entity.Stocks;

[Table("Stock")]
public class Stock
{
    [Key]
    public int StockCode { get; set; } // Khóa chính (PK)
    public int SpecCode { get; set; } // Liên kết với bảng Species (nếu có)
    [MaxLength(255)] public string SynOC { get; set; } = string.Empty;// Tên đồng nghĩa gốc
    public string StockDefs { get; set; } = string.Empty;// Định nghĩa Stock
    public string StockDefsGeneral { get; set; } = string.Empty;
    [MaxLength(50)]
    public string Level { get; set; } = string.Empty;
    public bool LocalUnique { get; set; } 
    
    // Navigation Properties (Liên kết 1-1 tới các bảng con bên dưới)
    public virtual StockConservation? Conservation { get; set; }
    public virtual StockEnvironment? Environment { get; set; }
    public virtual StockExternalRef? ExternalRefs { get; set; }
    public virtual StockDataAvailability? DataFlags { get; set; }
    public virtual StockMetadata? Metadata { get; set; }
}