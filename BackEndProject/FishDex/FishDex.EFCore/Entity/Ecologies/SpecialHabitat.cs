namespace FishDex.EFCore.Entity.Ecologies;

public class SpecialHabitat
{
    public int SpecialHabitatId { get; set; }
    public int EcologyId { get; set; }
    public string? SpecialHabitatRef { get; set; }
    public bool Macrophyte { get; set; }
    public bool BedsBivalve { get; set; }
    public bool BedsRock { get; set; }
    public bool SeaGrassBeds { get; set; }
    public bool BedsOthers { get; set; }
    public bool CoralReefs { get; set; }
    public bool ReefExclusive { get; set; }
    public bool DropOffs { get; set; }
    public bool ReefFlats { get; set; }
    public bool Lagoons { get; set; }
    public bool Burrows { get; set; }
    public bool Tunnels { get; set; }
    public bool Guyots { get; set; }
    public bool Crevices { get; set; }
    public bool Seamounts { get; set; }
    public bool ColdSeeps { get; set; }
    public bool HydrothermalVents { get; set; }
    public bool DeepWaterCorals { get; set; }
    public bool Vegetation { get; set; }
    public bool Leaves { get; set; }
    public bool Stems { get; set; }
    public bool Roots { get; set; }
    public bool Driftwood { get; set; }
    public bool OInverterbrates { get; set; }
    public string? OIRemarks { get; set; }
    public bool Verterbrates { get; set; }
    public string? VRemarks { get; set; }
    public bool Pilings { get; set; }
    public bool RicePaddies { get; set; }
    public bool BoatHulls { get; set; }
    public bool Corals { get; set; }
    public bool SoftCorals { get; set; }
    public bool OnPolyp { get; set; }
    public bool BetweenPolyps { get; set; }
    public bool HardCorals { get; set; }
    public bool OnExoskeleton { get; set; }
    public bool InterstitialSpaces { get; set; }

    public Ecology Ecology { get; set; }
}
