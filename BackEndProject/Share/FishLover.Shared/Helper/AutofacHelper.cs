using System.Reflection;
using Autofac;

namespace FishLover.Shared.Helper;

public static class AutofacHelper
{
    /// <summary>
    /// Register services with Scoped lifetime (per request)
    /// Convention: Classes ending with "Service", "Repository", "Manager"
    /// </summary>
    public static void RegisterScopedServices(ContainerBuilder builder, Assembly assembly)
    {
        builder.RegisterAssemblyTypes(assembly)
            .Where(t => !t.IsAbstract && 
                       (t.Name.EndsWith("Service") || 
                        t.Name.EndsWith("Repository") || 
                        t.Name.EndsWith("Manager")))
            .AsImplementedInterfaces()
            .InstancePerLifetimeScope(); // Scoped
    }

    /// <summary>
    /// Register services with Singleton lifetime (single instance for entire app)
    /// Convention: Classes ending with "SingletonService", "SingletonManager", "SingletonRepository"
    /// </summary>
    public static void RegisterSingletonServices(ContainerBuilder builder, Assembly assembly)
    {
        builder.RegisterAssemblyTypes(assembly)
            .Where(t => !t.IsAbstract && 
                       (t.Name.EndsWith("SingletonService") || 
                        t.Name.EndsWith("SingletonManager") || 
                        t.Name.EndsWith("SingletonRepository")))
            .AsImplementedInterfaces()
            .SingleInstance(); // Singleton
    }

    /// <summary>
    /// Register services with Transient lifetime (new instance every time)
    /// Convention: Classes ending with "Factory", "Provider", "Builder"
    /// </summary>
    public static void RegisterTransientServices(ContainerBuilder builder, Assembly assembly)
    {
        builder.RegisterAssemblyTypes(assembly)
            .Where(t => !t.IsAbstract && 
                       (t.Name.EndsWith("Factory") || 
                        t.Name.EndsWith("Provider") || 
                        t.Name.EndsWith("Builder")))
            .AsImplementedInterfaces()
            .InstancePerDependency(); // Transient
    }

    /// <summary>
    /// Register all services from assembly using conventions
    /// - Scoped: Service, Repository, Manager
    /// - Singleton: SingletonService, SingletonManager, SingletonRepository
    /// - Transient: Factory, Provider, Builder
    /// </summary>
    public static void LoadAssembly(ContainerBuilder builder, Assembly assembly)
    {
        RegisterScopedServices(builder, assembly);
        RegisterSingletonServices(builder, assembly);
        RegisterTransientServices(builder, assembly);
    }
    
    /// <summary>
    /// Register multiple assemblies at once
    /// </summary>
    public static void LoadAssemblies(ContainerBuilder builder, params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            LoadAssembly(builder, assembly);
        }
    }
}