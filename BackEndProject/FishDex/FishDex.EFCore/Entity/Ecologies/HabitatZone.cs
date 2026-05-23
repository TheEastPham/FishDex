namespace FishDex.EFCore.Entity.Ecologies;

public class HabitatZone
{
    public int HabitatZoneId { get; set; }
    public int EcologyId { get; set; }
    public string? HabitatsRef { get; set; }
    public bool Neritic { get; set; }
    public bool SupraLittoralZone { get; set; }
    public bool Saltmarshes { get; set; }
    public bool LittoralZone { get; set; }
    public bool TidePools { get; set; }
    public bool Intertidal { get; set; }
    public bool SubLittoral { get; set; }
    public bool Caves { get; set; }
    public bool Oceanic { get; set; }
    public bool Epipelagic { get; set; }
    public bool Mesopelagic { get; set; }
    public bool Bathypelagic { get; set; }
    public bool Abyssopelagic { get; set; }
    public bool Hadopelagic { get; set; }
    public bool Estuaries { get; set; }
    public bool Mangroves { get; set; }
    public bool MarshesSwamps { get; set; }
    public bool CaveAnchialine { get; set; }
    public bool Stream { get; set; }
    public bool Lakes { get; set; }
    public bool Cave { get; set; }
    public bool Cave2 { get; set; }

    public Ecology Ecology { get; set; }
}
