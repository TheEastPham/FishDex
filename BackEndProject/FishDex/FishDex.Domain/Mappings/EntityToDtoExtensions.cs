using FishDex.Domain.DTOs.Ecologies;
using FishDex.Domain.Helpers;
using FishDex.Domain.DTOs.Media;
using FishDex.Domain.DTOs.Occurrence;
using FishDex.Domain.DTOs.Species;
using FishDex.EFCore.Entity.Ecologies;
using FishDex.EFCore.Entity.Media;
using FishDex.EFCore.Entity.Occurrence;
using FishDex.EFCore.Entity.Species;

namespace FishDex.Domain.Mappings;

internal static class EntityToDtoExtensions
{
    internal static FamilyDto ToDto(this Family e) => new()
    {
        Id        = e.Id,
        FamCode   = e.FamCode,
        Name      = e.Name,
        CommonName = e.CommonName
    };

    internal static GenusDto ToDto(this Genus e) => new()
    {
        GenusCode = e.GenusCode,
        GenusName = e.GenusName,
        FamId     = e.FamId
    };

    internal static OccurrenceDto ToDto(this Occurrence e) => new()
    {
        Id           = e.Id,
        SpecCode     = e.SpecCode,
        CountryCode  = e.CountryCode,
        Locality     = e.Locality,
        LatitudeDec  = e.LatitudeDec,
        LongitudeDec = e.LongitudeDec,
        Province     = e.Province
    };

    internal static SystemImageDto ToDto(this SystemImage e) => new()
    {
        Id           = e.Id,
        SpecCode     = e.SpecCode,
        Name         = e.Name,
        PictureType  = e.PictureType,
        PicPreferred = e.PicPreferred,
        Gender       = e.PicPreferredMale == true ? ImageGender.Male
                     : e.PicPreferredFem  == true ? ImageGender.Female
                     : e.PicPreferredJuv  == true ? ImageGender.Juvenile
                     : ImageGender.Unknown
    };

    internal static SubstrateDto ToDto(this Substrate e) => new()
    {
        EcologyId           = e.EcologyId,
        PreferredSubstrates = HabitatMappingHelper.MapSubstrates(e),
        BurrowingCapable    = e.Endofauna
    };

    internal static SpecialHabitatDto ToDto(this SpecialHabitat e) => new()
    {
        EcologyId          = e.EcologyId,
        SpecialHabitats    = HabitatMappingHelper.MapSpecialHabitats(e),
        RequiresCaves      = e.Burrows || e.Crevices || e.Tunnels,
        RequiresDriftwood  = e.Driftwood,
        RequiresVegetation = e.Macrophyte || e.Vegetation || e.Leaves || e.Stems || e.Roots,
        RequiresCoralReefs = e.CoralReefs || e.ReefExclusive
    };
}
