using Autofac;
using AquaHome.EFCore.Repository;
using AquaHome.EFCore.Repository.Interface;

namespace AquaHome.EFCore.Modules;

public class AquaHomeEFCoreModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<AquariumRepository>().As<IAquariumRepository>().InstancePerLifetimeScope();
        builder.RegisterType<UserFavoriteRepository>().As<IUserFavoriteRepository>().InstancePerLifetimeScope();
    }
}
