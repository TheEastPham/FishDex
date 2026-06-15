using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using UserManagement.EFCore.Data;

namespace UserManagement.EFCore
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<UserManagementDbContext>
    {
        public UserManagementDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<UserManagementDbContext>();
            
            var connectionString =
                Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Set ConnectionStrings__DefaultConnection env var for EF design-time operations.");
            optionsBuilder.UseNpgsql(connectionString);

            return new UserManagementDbContext(optionsBuilder.Options);
        }
    }
}
