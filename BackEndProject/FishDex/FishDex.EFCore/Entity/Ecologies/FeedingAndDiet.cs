namespace FishDex.EFCore.Entity.Ecologies;

public class FeedingAndDiet
{
    public int FeedingId { get; set; }
    public int EcologyId { get; set; }
    public bool Herbivory2 { get; set; }
    public string HerbivoryRef { get; set; }
    public string FeedingType { get; set; }
    public string FeedingTypeRef { get; set; }
    public decimal DietTroph { get; set; }
    public decimal DietSeTroph { get; set; }
    public decimal DietTLu { get; set; }
    public decimal DietseTLu { get; set; }
    public string DietRemark { get; set; }
    public string DietRef { get; set; }
    public decimal FoodTroph { get; set; }
    public decimal FoodSeTroph { get; set; }
    public string FoodRemark { get; set; }
    public string FoodRef { get; set; }
    public string AddRems { get; set; }
    
    // Foreign key reference
    public Ecology Ecology { get; set; }
}