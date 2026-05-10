// Bảng thông tin về tình trạng bảo tồn và pháp lý

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FishDex.EFCore.Entity.Stocks;

[Table("StockConservation")]
public class StockConservation
{
    [Key, ForeignKey("Stock")]
    public int StockCode { get; set; } // PK đồng thời là FK trỏ về Stock

    [MaxLength(50)]
    public string IUCN_Code { get; set; }
    
    public string IUCN_Assessment { get; set; }
    public DateTime? IUCN_DateAssessed { get; set; }
    public int? IUCN_ID { get; set; }
    public int? IUCN_IDAssess { get; set; }

    public bool Protected { get; set; }
    
    [MaxLength(50)]
    public string CITES_Code { get; set; }
    public DateTime? CITES_Date { get; set; }
    public string CITES_Ref { get; set; }
    public string CITES_Remarks { get; set; }
    
    [MaxLength(50)]
    public string CMS { get; set; }

    public virtual Stock Stock { get; set; }
}