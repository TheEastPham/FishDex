using UserManagement.EFCore.Entities.User;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UserManagement.EFCore.Entities.Invitation;

namespace UserManagement.EFCore.Data;

public class UserManagementDbContext(DbContextOptions<UserManagementDbContext> options)
    : IdentityDbContext<UserEntity, RoleEntity, Guid>(options)
{
    public DbSet<UserProfileEntity> UserProfiles { get; set; }
    public DbSet<Invitation> Invitations { get; set; }
    public DbSet<InvitationUsed> InvitationUsages { get; set; }


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure UserEntity
        builder.Entity<UserEntity>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.Avatar).HasMaxLength(500);
            entity.Property(e => e.TimeZone).HasMaxLength(50);
            entity.Property(e => e.Language).HasMaxLength(10);
            entity.Property(e => e.RefreshToken).HasMaxLength(500);
        });

        // Configure RoleEntity
        builder.Entity<RoleEntity>(entity =>
        {
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        // Configure UserProfileEntity
        builder.Entity<UserProfileEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.Bio).HasMaxLength(1000);
            entity.Property(e => e.Preferences).HasMaxLength(2000);
            
            // Relationship with UserEntity
            entity.HasOne(e => e.User)
                .WithOne(u => u.Profile)
                .HasForeignKey<UserProfileEntity>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure RoleEntity
        builder.Entity<RoleEntity>(entity =>
        {
            entity.Property(e => e.Description).HasMaxLength(500);
        });
        
        builder.Entity<Invitation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
    
            entity.HasMany(e => e.UsedBy)
                .WithOne()
                .HasForeignKey(u => u.InvitationId);
        });

        // Seed default data
        SeedDefaultRoles(builder);
    }

    private static void SeedDefaultRoles(ModelBuilder builder)
    {
        var roles = new[]
        {
            new RoleEntity
            {
                Id = Guid.NewGuid(),
                Name = "SystemAdmin",
                NormalizedName = "SYSTEMADMIN",
                Description = "System Administrator with full system access",
                CreatedAt = DateTime.UtcNow
            },
            new RoleEntity
            {
                Id = Guid.NewGuid(),
                Name = "ContentAdmin",
                NormalizedName = "CONTENTADMIN",
                Description = "Content Administrator with content management permissions",
                CreatedAt = DateTime.UtcNow
            },
            new RoleEntity
            {
                Id = Guid.NewGuid(),
                Name = "Member",
                NormalizedName = "MEMBER",
                Description = "Regular user with basic permissions",
                CreatedAt = DateTime.UtcNow
            }
        };

        builder.Entity<RoleEntity>().HasData(roles);
    }
}
