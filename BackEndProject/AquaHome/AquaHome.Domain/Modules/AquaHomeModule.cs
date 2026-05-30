using Autofac;
using AquaHome.Domain.Services;
using AquaHome.Domain.Services.Interfaces;

namespace AquaHome.Domain.Modules;

public class AquaHomeModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<AquariumService>().As<IAquariumService>().InstancePerLifetimeScope();
        builder.RegisterType<FavoriteService>().As<IFavoriteService>().InstancePerLifetimeScope();
    }
}
