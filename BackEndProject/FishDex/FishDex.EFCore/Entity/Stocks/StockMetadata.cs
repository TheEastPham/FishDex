// Bảng Audit log (Thông tin quản trị)

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FishDex.EFCore.Entity.Stocks;

[Table("StockMetadata")]
public class StockMetadata
{
    [Key, ForeignKey("Stock")]
    public int StockCode { get; set; }

    public int? Entered { get; set; } // ID người nhập
    public DateTime? DateEntered { get; set; }
    
    public int? Modified { get; set; } // ID người sửa
    public DateTime? DateModified { get; set; }
    
    public int? Expert { get; set; } // ID chuyên gia duyệt
    public DateTime? DateChecked { get; set; }
    
    public DateTime? TS { get; set; } // Timestamp

    public virtual Stock Stock { get; set; }
}