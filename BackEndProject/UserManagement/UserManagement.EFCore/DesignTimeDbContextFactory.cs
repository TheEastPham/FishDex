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
            
            // Use default connection string for design time
            var connectionString = "Host=localhost;Port=5435;Database=UserManagement;Username=usermanagement;Password=UserMgmt_Local_Pwd1!";
            optionsBuilder.UseNpgsql(connectionString);

            return new UserManagementDbContext(optionsBuilder.Options);
        }
    }
}
