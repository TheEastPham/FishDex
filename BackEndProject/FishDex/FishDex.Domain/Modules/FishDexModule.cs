using Autofac;
using FishDex.Domain.Services;
using FishDex.Domain.Services.Interfaces;

namespace FishDex.Domain.Modules;

public class FishDexModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<SpeciesService>().As<ISpeciesService>().InstancePerLifetimeScope();
        builder.RegisterType<EcologyService>().As<IEcologyService>().InstancePerLifetimeScope();
        builder.RegisterType<StockService>().As<IStockService>().InstancePerLifetimeScope();
        builder.RegisterType<MorphDataService>().As<IMorphDataService>().InstancePerLifetimeScope();
        builder.RegisterType<EcosystemService>().As<IEcosystemService>().InstancePerLifetimeScope();
        builder.RegisterType<OccurrenceService>().As<IOccurrenceService>().InstancePerLifetimeScope();
        builder.RegisterType<MediaService>().As<IMediaService>().InstancePerLifetimeScope();
    }
}
