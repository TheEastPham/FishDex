using FishDex.Domain.DTOs.Ecologies;
using FishDex.Domain.DTOs.Ecosystem;
using FishDex.Domain.DTOs.Media;
using FishDex.Domain.DTOs.MorphData;
using FishDex.Domain.DTOs.Occurrence;
using FishDex.Domain.DTOs.Species;
using FishDex.Domain.DTOs.Stocks;
using FishDex.EFCore.Entity.Ecologies;
using FishDex.EFCore.Entity.Ecosystem;
using FishDex.EFCore.Entity.Media;
using FishDex.EFCore.Entity.MorphData;
using FishDex.EFCore.Entity.Occurrence;
using FishDex.EFCore.Entity.Species;
using FishDex.EFCore.Entity.Stocks;

namespace FishDex.Domain.Mappings;

internal static class EntityToDtoExtensions
{
    internal static CommonNameDto ToDto(this CommonName e) => new()
    {
        AutoCtr        = e.AutoCtr,
        SpecCode       = e.SpecCode,
        ComName        = e.ComName,
        Transliteration = e.Transliteration,
        Language       = e.Language,
        CountryCode    = e.CountryCode,
        NameType       = e.NameType,
        IsPreferred    = e.IsPreferred,
        Rank           = e.Rank
    };

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

    internal static SpeciesDto ToDto(this Species e) => new()
    {
        Id          = e.Id,
        SpecCode    = e.SpecCode,
        SpeciesName = e.SpeciesName,
        GenusCode   = e.GenusCode,
        GenusName   = null,
        FamilyName  = null
    };

    internal static SpeciesSearchResultDto ToSearchResultDto(this Species e, string? language = null, string? imageUrl = null) => new()
    {
        SpecCode            = e.SpecCode,
        SpeciesName         = e.SpeciesName,
        PreferredCommonName = e.CommonNames.PickPreferredName(language),
        GenusName           = e.Genus?.GenusName,
        FamilyName          = e.Family?.Name,
        ImageUrl            = imageUrl
    };

    private static string? PickPreferredName(this ICollection<CommonName> names, string? language)
    {
        static string? Best(IEnumerable<CommonName> pool) =>
            pool.OrderByDescending(c => c.IsPreferred).ThenBy(c => c.Rank).FirstOrDefault()?.ComName;

        // 1. Ưu tiên ngôn ngữ yêu cầu
        if (language != null)
        {
            var inLang = Best(names.Where(c => c.Language == language));
            if (inLang != null) return inLang;
        }

        // 2. Fallback về English
        return Best(names.Where(c => c.Language == "English"));
    }

    internal static EcologyDto ToDto(this Ecology e) => new()
    {
        EcologyId = e.EcologyId,
        SpecCode  = e.SpecCode,
        StockCode = e.StockCode
    };

    internal static FeedingAndDietDto ToDto(this FeedingAndDiet e) => new()
    {
        FeedingId   = e.FeedingId,
        EcologyId   = e.EcologyId,
        FeedingType = e.FeedingType,
        DietTroph   = e.DietTroph,
        FoodTroph   = e.FoodTroph
    };

    internal static HabitatZoneDto ToDto(this HabitatZone e) => new()
    {
        HabitatZoneId = e.HabitatZoneId,
        EcologyId     = e.EcologyId,
        Neritic       = e.Neritic,
        Estuaries     = e.Estuaries,
        Mangroves     = e.Mangroves,
        Stream        = e.Stream,
        Lakes         = e.Lakes
    };

    internal static MorphDataDto ToDto(this MorphData e) => new()
    {
        StockCode   = e.StockCode,
        Speccode    = e.Speccode,
        BodyShapeI  = e.BodyShapeI,
        BodyShapeII = e.BodyShapeII,
        TypeofMouth = e.TypeofMouth
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

    internal static StockDto ToDto(this Stock e) => new()
    {
        StockCode = e.StockCode,
        SpecCode  = e.SpecCode,
        StockDefs = e.StockDefs,
        Level     = e.Level
    };

    internal static StockConservationDto ToDto(this StockConservation e) => new()
    {
        StockCode         = e.StockCode,
        IUCN_Code         = e.IUCN_Code,
        IUCN_Assessment   = e.IUCN_Assessment,
        IUCN_DateAssessed = e.IUCN_DateAssessed,
        CITES_Code        = e.CITES_Code
    };

    internal static StockEnvironmentDto ToDto(this StockEnvironment e) => new()
    {
        StockCode = e.StockCode,
        TempMin   = e.TempMin,
        TempMax   = e.TempMax,
        PHMin     = e.PHMin,
        PHMax     = e.PHMax
    };

    internal static EcosystemRefDto ToDto(this EcosystemRef e) => new()
    {
        E_CODE        = e.E_CODE,
        EcosystemName = e.EcosystemName,
        EcosystemType = e.EcosystemType,
        Location      = e.Location,
        NorthernLat   = e.NorthernLat,
        SouthernLat   = e.SouthernLat,
        WesternLat    = e.WesternLat,
        EasternLat    = e.EasternLat,
        Area          = e.Area,
        DrainageArea  = e.DrainageArea,
        RiverLength   = e.RiverLength,
        Salinity      = e.Salinity,
        AverageDepth  = e.AverageDepth,
        MaxDepth      = e.MaxDepth,
        TempSurface   = e.TempSurface,
        TempDepth     = e.TempDepth,
        Polar         = e.Polar,
        Boreal        = e.Boreal,
        Temperate     = e.Temperate,
        Subtropical   = e.Subtropical,
        Tropical      = e.Tropical,
        MEOW          = e.MEOW,
        LME           = e.LME,
        MPA           = e.MPA,
        TotalCount    = e.TotalCount,
        TotalFamCount = e.TotalFamCount,
        Description   = e.Description,
        Comments      = e.Comments,
        LastUpdate    = e.LastUpdate
    };

    internal static EcosystemDto ToDto(this FishDex.EFCore.Entity.Ecosystem.Ecosystem e) => new()
    {
        AutoCtr         = e.AutoCtr,
        E_CODE          = e.E_CODE,
        SpecCode        = e.SpecCode,
        StockCode       = e.StockCode,
        Status          = e.Status,
        CurrentPresence = e.CurrentPresence,
        Abundance       = e.Abundance,
        LifeStage       = e.LifeStage,
        Remarks         = e.Remarks
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
}
