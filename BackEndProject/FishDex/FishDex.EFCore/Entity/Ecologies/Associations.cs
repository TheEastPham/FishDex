namespace FishDex.EFCore.Entity.Ecologies;

public class Associations
{
    public int AssociationId { get; set; }
    public int EcologyId { get; set; }
    public string AssociationRef { get; set; }
    public bool Parasitism { get; set; }
    public bool Solitary { get; set; }
    public bool Symbiosis { get; set; }
    public bool Symphorism { get; set; }
    public bool Commensalism { get; set; }
    public bool Mutualism { get; set; }
    public bool Epiphytic { get; set; }
    public bool Schooling { get; set; }
    public string SchoolingFrequency { get; set; }
    public string SchoolingLifestage { get; set; }
    public bool Shoaling { get; set; }
    public string ShoalingFrequency { get; set; }
    public string ShoalingLifestage { get; set; }
    public string SchoolShoalRef { get; set; }
    public string AssociationsWith { get; set; }
    public string AssociationsRemarks { get; set; }
    public bool OutsideHost { get; set; }
    public string OHRemarks { get; set; }
    public bool InsideHost { get; set; }
    public string IHRemarks { get; set; }
    
    // Foreign key reference
    public Ecology Ecology { get; set; }
}