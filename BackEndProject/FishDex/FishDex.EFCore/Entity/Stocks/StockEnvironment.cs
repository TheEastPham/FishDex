using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FishDex.EFCore.Enum;

// Bảng thông tin địa lý và điều kiện môi trường sống
namespace FishDex.EFCore.Entity.Stocks;

[Table("StockEnvironment")]
public class StockEnvironment
{
    [Key, ForeignKey("Stock")]
    public int StockCode { get; set; }

    // Coordinates (Bounding Box)
    public double? Northernmost { get; set; }
    public string NorthSouthN { get; set; }
    public double? Southermost { get; set; }
    public string NorthSouthS { get; set; }
    public double? Westernmost { get; set; }
    public string WestEastW { get; set; }
    public double? Easternmost { get; set; }
    public string WestEastE { get; set; }
    
    public string BoundingRef { get; set; }
    public string BoundingMethod { get; set; }

    // Temperature
    public double? TempMin { get; set; }
    public double? TempMax { get; set; }
    public double? TempPreferred { get; set; }
    public double? TempPref25 { get; set; }
    public double? TempPref50 { get; set; }
    public double? TempPref75 { get; set; }
    public double? EnvTemp { get; set; }

    // Water Chemistry (pH, dH)
    public double? PHMin { get; set; }
    public double? PHMax { get; set; }
    public double? DHMin { get; set; }
    public double? DHMax { get; set; }

    public ResilienceLevel? Resilience { get; set; }
    public string ResilienceRemark { get; set; }

    public virtual Stock Stock { get; set; }
}