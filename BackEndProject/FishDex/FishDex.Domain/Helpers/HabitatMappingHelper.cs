using FishDex.EFCore.Entity.Ecologies;

namespace FishDex.Domain.Helpers;

public static class HabitatMappingHelper
{
    public static IReadOnlyList<string> MapSubstrates(Substrate e)
    {
        var list = new List<string>();

        if (e.Sand)       list.Add("Sand");
        if (e.Gravel)     list.Add("Gravel");
        if (e.Rocky)      list.Add("Rocky");
        if (e.Rubble)     list.Add("Rubble");
        if (e.Coarse)     list.Add("Coarse");
        if (e.Fine)       list.Add("Fine");
        if (e.Mud)        list.Add("Mud");
        if (e.Silt)       list.Add("Silt");
        if (e.Ooze)       list.Add("Ooze");
        if (e.SoftBottom) list.Add("SoftBottom");
        if (e.HardBottom) list.Add("HardBottom");
        if (e.Detritus)   list.Add("Detritus");
        if (e.Organic)    list.Add("Organic");
        if (e.Pelagic)    list.Add("Pelagic");
        if (e.Benthic)    list.Add("Benthic");
        if (e.Demersal)   list.Add("Demersal");

        return list;
    }

    public static IReadOnlyList<string> MapSpecialHabitats(SpecialHabitat e)
    {
        var list = new List<string>();

        if (e.CoralReefs || e.ReefExclusive) list.Add("CoralReefs");
        if (e.ReefFlats)                     list.Add("ReefFlats");
        if (e.Lagoons)                       list.Add("Lagoons");
        if (e.DropOffs)                      list.Add("DropOffs");
        if (e.Burrows || e.Crevices)         list.Add("Burrows");
        if (e.Tunnels)                       list.Add("Tunnels");
        if (e.SeaGrassBeds)                  list.Add("SeaGrassBeds");
        if (e.Macrophyte || e.Vegetation)    list.Add("Vegetation");
        if (e.Leaves || e.Stems || e.Roots)  list.Add("AquaticPlants");
        if (e.Driftwood)                     list.Add("Driftwood");
        if (e.DeepWaterCorals)               list.Add("DeepWaterCorals");
        if (e.HydrothermalVents)             list.Add("HydrothermalVents");
        if (e.ColdSeeps)                     list.Add("ColdSeeps");
        if (e.Seamounts)                     list.Add("Seamounts");
        if (e.RicePaddies)                   list.Add("RicePaddies");

        return list;
    }
}
