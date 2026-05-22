using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FishDex.EFCore.Entity.Ecosystem;

/// <summary>
/// ecosystemref.csv — bảng tham chiếu hệ sinh thái (sông, hồ, biển, v.v.)
/// </summary>
[Table("EcosystemRef")]
public class EcosystemRef
{
    [Key]
    public int E_CODE { get; set; }

    [MaxLength(255)]
    public string? EcosystemName { get; set; }

    [MaxLength(50)]
    public string? EcosystemType { get; set; }   // e.g. River, Lake, Marine, Estuary

    [MaxLength(255)]
    public string? Location { get; set; }

    // ─── Bounding Box ────────────────────────────────────────────
    public double? NorthernLat { get; set; }
    public double? SouthernLat { get; set; }
    public double? WesternLat { get; set; }
    public double? EasternLat { get; set; }

    // ─── Physical Properties ─────────────────────────────────────
    public double? Area { get; set; }           // km²
    public double? DrainageArea { get; set; }   // km²
    public double? RiverLength { get; set; }    // km
    public double? Salinity { get; set; }

    // ─── Depth & Temperature ─────────────────────────────────────
    public double? AverageDepth { get; set; }   // m
    public double? MaxDepth { get; set; }        // m
    public double? TempSurface { get; set; }    // °C
    public double? TempDepth { get; set; }      // °C

    // ─── Climate Zones (bool flags) ──────────────────────────────
    public bool Polar { get; set; }
    public bool Boreal { get; set; }
    public bool Temperate { get; set; }
    public bool Subtropical { get; set; }
    public bool Tropical { get; set; }

    // ─── Marine Classifications ───────────────────────────────────
    [MaxLength(100)]
    public string? MEOW { get; set; }           // Marine Ecoregions of the World
    [MaxLength(100)]
    public string? LME { get; set; }            // Large Marine Ecosystem
    [MaxLength(100)]
    public string? MPA { get; set; }            // Marine Protected Area

    // ─── Species Count ────────────────────────────────────────────
    public int? TotalCount { get; set; }
    public int? TotalFamCount { get; set; }

    public string? Description { get; set; }
    public string? Comments { get; set; }

    public DateTime? LastUpdate { get; set; }

    // ─── Navigation ───────────────────────────────────────────────
    public virtual ICollection<Ecosystem> Occurrences { get; set; } = [];
}
