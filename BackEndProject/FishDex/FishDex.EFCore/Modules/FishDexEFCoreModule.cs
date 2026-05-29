using Autofac;
using FishDex.Domain.Services.Interfaces;
using FishDex.EFCore.Repository;
using FishDex.EFCore.Repository.Interface;
using FishDex.EFCore.Storage;

namespace FishDex.EFCore.Modules;

public class FishDexEFCoreModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<SpeciesRepository>()            .As<ISpeciesRepository>()            .InstancePerLifetimeScope();
        builder.RegisterType<FamiliesRepository>()           .As<IFamiliesRepository>()           .InstancePerLifetimeScope();
        builder.RegisterType<GenusRepository>()              .As<IGenusRepository>()              .InstancePerLifetimeScope();
        builder.RegisterType<CommonNameRepository>()         .As<ICommonNameRepository>()         .InstancePerLifetimeScope();
        builder.RegisterType<StockRepository>()              .As<IStockRepository>()              .InstancePerLifetimeScope();
        builder.RegisterType<StockConservationRepository>()  .As<IStockConservationRepository>()  .InstancePerLifetimeScope();
        builder.RegisterType<StockEnvironmentRepository>()   .As<IStockEnvironmentRepository>()   .InstancePerLifetimeScope();
        builder.RegisterType<StockExternalRefRepository>()   .As<IStockExternalRefRepository>()   .InstancePerLifetimeScope();
        builder.RegisterType<StockDataAvailabilityRepository>().As<IStockDataAvailabilityRepository>().InstancePerLifetimeScope();
        builder.RegisterType<StockMetadataRepository>()      .As<IStockMetadataRepository>()      .InstancePerLifetimeScope();
        builder.RegisterType<EcologyRepository>()            .As<IEcologyRepository>()            .InstancePerLifetimeScope();
        builder.RegisterType<HabitatZoneRepository>()        .As<IHabitatZoneRepository>()        .InstancePerLifetimeScope();
        builder.RegisterType<FeedingAndDietRepository>()     .As<IFeedingAndDietRepository>()     .InstancePerLifetimeScope();
        builder.RegisterType<AssociationsRepository>()       .As<IAssociationsRepository>()       .InstancePerLifetimeScope();
        builder.RegisterType<SubstrateRepository>()          .As<ISubstrateRepository>()          .InstancePerLifetimeScope();
        builder.RegisterType<SpecialHabitatRepository>()     .As<ISpecialHabitatRepository>()     .InstancePerLifetimeScope();
        builder.RegisterType<CircadianBehaviorRepository>()  .As<ICircadianBehaviorRepository>()  .InstancePerLifetimeScope();
        builder.RegisterType<MorphDataRepository>()          .As<IMorphDataRepository>()          .InstancePerLifetimeScope();
        builder.RegisterType<MorphTeethRepository>()         .As<IMorphTeethRepository>()         .InstancePerLifetimeScope();
        builder.RegisterType<MorphPigmentationRepository>()  .As<IMorphPigmentationRepository>()  .InstancePerLifetimeScope();
        builder.RegisterType<MorphFinsRepository>()          .As<IMorphFinsRepository>()          .InstancePerLifetimeScope();
        builder.RegisterType<MorphMeristicsRepository>()     .As<IMorphMeristicsRepository>()     .InstancePerLifetimeScope();
        builder.RegisterType<MorphMetricsRepository>()       .As<IMorphMetricsRepository>()       .InstancePerLifetimeScope();
        builder.RegisterType<OccurrenceRepository>()         .As<IOccurrenceRepository>()         .InstancePerLifetimeScope();
        builder.RegisterType<EcosystemRepository>()          .As<IEcosystemRepository>()          .InstancePerLifetimeScope();
        builder.RegisterType<EcosystemRefRepository>()       .As<IEcosystemRefRepository>()       .InstancePerLifetimeScope();
        builder.RegisterType<SystemImageRepository>()        .As<ISystemImageRepository>()        .InstancePerLifetimeScope();
        builder.RegisterType<S3StorageService>()              .As<IStorageService>()               .SingleInstance();
    }
}
