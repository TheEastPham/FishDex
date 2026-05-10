using System.Reflection;
using Autofac;
using FishLover.Shared.Helper;

namespace UserManagement.EFCore.Modules;

/// <summary>
/// Autofac module for UserManagement EFCore layer
/// Auto-registers all Repositories from this assembly
/// </summary>
public class UserManagementEfCoreModule : Autofac.Module
{
    protected override void Load(ContainerBuilder builder)
    {
        var assembly = Assembly.GetExecutingAssembly();
        
        // Auto-register repositories using convention-based approach
        // This will register: UserRepository, InvitationRepository, etc.
        AutofacHelper.RegisterScopedServices(builder, assembly);
    }
}

