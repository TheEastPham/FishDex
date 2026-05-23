using System.ComponentModel.DataAnnotations;

namespace FishDex.EFCore.Entity.Ecologies;

public class CircadianBehavior
{
    [Key]
    public int CircadianId { get; set; }
    public int EcologyId { get; set; }
    public string? Circadian1 { get; set; }
    public string? Circadian2 { get; set; }
    public string? Circadian3 { get; set; }
    public string? BioAspect1 { get; set; }
    public string? BioAspect2 { get; set; }
    public string? BioAspect3 { get; set; }
    public string? RemarksCircadian { get; set; }
    public string? CircadianRef { get; set; }
    public string? CircadianAlsoRef { get; set; }

    public Ecology Ecology { get; set; }
}
