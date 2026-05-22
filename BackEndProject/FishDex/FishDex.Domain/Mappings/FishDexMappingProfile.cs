using AutoMapper;
using FishDex.Domain.DTOs.Ecologies;
using FishDex.Domain.DTOs.Media;
using FishDex.Domain.DTOs.MorphData;
using FishDex.Domain.DTOs.Occurrence;
using FishDex.Domain.DTOs.Species;
using FishDex.Domain.DTOs.Stocks;
using FishDex.EFCore.Entity.Ecologies;
using FishDex.EFCore.Entity.Media;
using FishDex.EFCore.Entity.MorphData;
using FishDex.EFCore.Entity.Occurrence;
using FishDex.EFCore.Entity.Species;
using FishDex.EFCore.Entity.Stocks;

namespace FishDex.Domain.Mappings;

public class FishDexMappingProfile : Profile
{
    public FishDexMappingProfile()
    {
        // Species
        CreateMap<Family, FamilyDto>();
        CreateMap<Genus, GenusDto>();
        CreateMap<Species, SpeciesDto>();

        // Ecologies
        CreateMap<Ecology, EcologyDto>();
        CreateMap<FeedingAndDiet, FeedingAndDietDto>();
        CreateMap<HabitatZone, HabitatZoneDto>();

        // MorphData
        CreateMap<MorphData, MorphDataDto>();

        // Occurrence
        CreateMap<Occurrence, OccurrenceDto>();

        // Stocks
        CreateMap<Stock, StockDto>();
        CreateMap<StockConservation, StockConservationDto>();
        CreateMap<StockEnvironment, StockEnvironmentDto>();

        // Media
        CreateMap<SystemImage, SystemImageDto>();
    }
}
