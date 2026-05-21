namespace FishDex.EFCore.Entity.Ecologies;

public class Ecology
{
    public int EcologyId { get; set; }
    public int SpecCode { get; set; }
    public string StockCode { get; set; }
    public string EcologyRefNo { get; set; }
    public int autoctr { get; set; }
    
    // Metadata
    public string Entered { get; set; }
    public DateTime Dateentered { get; set; }
    public string Modified { get; set; }
    public DateTime Datemodified { get; set; }
    public string Expert { get; set; }
    public DateTime Datechecked { get; set; }
    public DateTime TS { get; set; }
    
    // Navigation properties
    public HabitatZone? HabitatZone { get; set; }
    public FeedingAndDiet? FeedingAndDiet { get; set; }
    public Associations? Associations { get; set; }
    public Substrate? Substrate { get; set; }
    public SpecialHabitat? SpecialHabitat { get; set; }
    public CircadianBehavior? CircadianBehavior { get; set; }
}