namespace FishDex.EFCore.Entity.Species;

public class Family
{
    public Guid Id { get; set; }
    public int FamCode { get; set; }
    public string Name { get; set; } = string.Empty; //Family
    public string CommonName { get; set; } = string.Empty;
    public string BodyShapeI { get; set; } = string.Empty;
    public string SwimMode { get; set; } = string.Empty;
    public string ReproductiveGuild  { get; set; } = string.Empty; //ReprGuild 
    
    public virtual ICollection<Genus> Genuses { get; set; }

}