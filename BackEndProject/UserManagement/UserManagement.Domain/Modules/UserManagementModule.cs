using System.Reflection;
using Autofac;
using FishLover.Shared.Helper;
using UserManagement.Domain.Helper;
using UserManagement.EFCore.Modules;
using Module = Autofac.Module;

namespace UserManagement.Domain.Modules;

/// <summary>
/// Autofac module for UserManagement Domain layer
/// Auto-registers all Services, Repositories, and Managers from this assembly
/// </summary>
public class UserManagementModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        var assembly = Assembly.GetExecutingAssembly();
        
        // Register EFCore layer (Repositories)
        builder.RegisterModule<UserManagementEfCoreModule>();
        
        // Auto-register services using convention-based approach
        // This will register: UserService, AuthService, RoleService, EmailService, etc.
        AutofacHelper.RegisterScopedServices(builder, assembly);
        
        // Register singleton services explicitly for special cases
        builder.RegisterType<EmailTemplateHelper>()
            .AsSelf()
            .SingleInstance();
    }
}

