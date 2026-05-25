using Microsoft.AspNetCore.Identity;
using UserManagement.EFCore.Entities.User;

namespace UserManagement.API.Services;

public class AdminSeeder(IServiceProvider serviceProvider, IConfiguration configuration, ILogger<AdminSeeder> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!configuration.GetValue<bool>("Seeding:SeedInitialAdmin"))
            return;

        var email = configuration["InitialAdmin:Email"];
        var password = configuration["InitialAdmin:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("SeedInitialAdmin is true but InitialAdmin:Email or InitialAdmin:Password is missing.");
            return;
        }

        await using var scope = serviceProvider.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<UserEntity>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<RoleEntity>>();

        const string adminRole = "Admin";
        if (!await roleManager.RoleExistsAsync(adminRole))
        {
            await roleManager.CreateAsync(new RoleEntity { Name = adminRole, Description = "System administrator" });
            logger.LogInformation("Created role: {Role}", adminRole);
        }

        if (await userManager.FindByEmailAsync(email) is not null)
            return;

        var user = new UserEntity
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = "Admin",
            IsActive = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            logger.LogError("Failed to create initial admin: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(user, adminRole);
        logger.LogInformation("Initial admin seeded: {Email}", email);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
