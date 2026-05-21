using System.ComponentModel.DataAnnotations;

namespace FishDex.EFCore.Entity.Species;

public class Genus
{
    [Key]
    public int GenusCode { get; set; }  // Primary key
    public Guid FamId { get; set; }
    public string GenusName { get; set; } = string.Empty;  // The genus name
    
    // Navigation property
    public virtual ICollection<Species> Species { get; set; }
}