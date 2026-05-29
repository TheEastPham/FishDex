using OpenIddict.Abstractions;

namespace UserManagement.API.Services;

public class OpenIddictSeeder(IServiceProvider serviceProvider) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        await UpsertAsync(manager, new OpenIddictApplicationDescriptor
        {
            ClientId = "FishDex_Swagger",
            ClientType = OpenIddictConstants.ClientTypes.Public,
            DisplayName = "FishDex Swagger UI",
            RedirectUris =
            {
                new Uri("http://localhost:8081/swagger/oauth2-redirect.html")
            },
            Permissions =
            {
                OpenIddictConstants.Permissions.Endpoints.Authorization,
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                OpenIddictConstants.Permissions.ResponseTypes.Code,
                OpenIddictConstants.Permissions.Scopes.Email,
                OpenIddictConstants.Permissions.Scopes.Profile,
                OpenIddictConstants.Permissions.Scopes.Roles,
                OpenIddictConstants.Permissions.Prefixes.Scope + "fishdex"
            }
        }, cancellationToken);

        await UpsertAsync(manager, new OpenIddictApplicationDescriptor
        {
            ClientId = "aquahome-fe",
            ClientType = OpenIddictConstants.ClientTypes.Public,
            DisplayName = "AquaHome FE",
            RedirectUris =
            {
                new Uri("http://localhost:5173/callback"),  // Vite dev server
                new Uri("http://localhost:3000/callback"),  // Docker
            },
            PostLogoutRedirectUris =
            {
                new Uri("http://localhost:5173"),
                new Uri("http://localhost:3000"),
            },
            Permissions =
            {
                OpenIddictConstants.Permissions.Endpoints.Authorization,
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.Endpoints.Revocation,
                OpenIddictConstants.Permissions.Endpoints.Logout,
                OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                OpenIddictConstants.Permissions.ResponseTypes.Code,
                OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.OpenId,
                OpenIddictConstants.Permissions.Scopes.Email,
                OpenIddictConstants.Permissions.Scopes.Profile,
                OpenIddictConstants.Permissions.Scopes.Roles,
            }
        }, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task UpsertAsync(
        IOpenIddictApplicationManager manager,
        OpenIddictApplicationDescriptor descriptor,
        CancellationToken cancellationToken)
    {
        if (await manager.FindByClientIdAsync(descriptor.ClientId!, cancellationToken) is null)
            await manager.CreateAsync(descriptor, cancellationToken);
    }
}
