using AquaHome.EFCore.Entity;
using Microsoft.EntityFrameworkCore;

namespace AquaHome.EFCore.Data;

public class AquaHomeDbContext(DbContextOptions<AquaHomeDbContext> options) : DbContext(options)
{
    public DbSet<Aquarium>       Aquariums       => Set<Aquarium>();
    public DbSet<AquariumFish>   AquariumFish    => Set<AquariumFish>();
    public DbSet<AquariumMedia>  AquariumMedia   => Set<AquariumMedia>();
    public DbSet<UserFavorite>   UserFavorites   => Set<UserFavorite>();
    public DbSet<RecentlyViewed> RecentlyViewed  => Set<RecentlyViewed>();
    public DbSet<AquariumTask>   AquariumTasks   => Set<AquariumTask>();
    public DbSet<RoleQuota>      RoleQuotas      => Set<RoleQuota>();
    public DbSet<QuotaUsage>     QuotaUsages     => Set<QuotaUsage>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Aquarium>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
e.Property(x => x.Description).HasMaxLength(500);
            e.Ignore(x => x.VolumeLiters);   // computed: L×W×H/1000, không lưu DB
        });

        model.Entity<AquariumFish>(e =>
        {
            e.HasKey(x => new { x.AquariumId, x.SpecCode });
            e.HasOne(x => x.Aquarium)
             .WithMany(a => a.Fish)
             .HasForeignKey(x => x.AquariumId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        model.Entity<UserFavorite>(e =>
        {
            e.HasKey(x => new { x.UserId, x.SpecCode });
        });

        model.Entity<RecentlyViewed>(e =>
        {
            e.HasKey(x => new { x.UserId, x.SpecCode });
            e.HasIndex(x => new { x.UserId, x.ViewedAt });
        });

        model.Entity<AquariumMedia>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.FileName).HasMaxLength(260).IsRequired();
            e.Property(x => x.ContentType).HasMaxLength(50).IsRequired();
            e.HasOne(x => x.Aquarium)
             .WithMany()
             .HasForeignKey(x => x.AquariumId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        model.Entity<AquariumTask>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.AquaTaskType).IsRequired();
            e.HasIndex(x => new { x.IsCompleted, x.Reminded, x.DueAt });
            e.HasIndex(x => new { x.AquariumId, x.IsCompleted });
            e.HasOne(x => x.Aquarium)
             .WithMany()
             .HasForeignKey(x => x.AquariumId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        model.Entity<RoleQuota>(e =>
        {
            e.HasKey(x => x.Role);
            e.Property(x => x.Role).HasMaxLength(50);
            // Seed mặc định (giá trị -1 = không giới hạn). UpdatedAt cố định để migration deterministic.
            var seededAt = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc);
            e.HasData(
                new RoleQuota { Role = "Guest",        MaxFavorites = 10,  MaxAquariums = 2,  SearchPerDay = 20,  AiQaPerDay = 3,  ImageSearchPerDay = 3,  UpdatedAt = seededAt },
                new RoleQuota { Role = "Member",       MaxFavorites = 100, MaxAquariums = 10, SearchPerDay = 115, AiQaPerDay = 15, ImageSearchPerDay = 20, UpdatedAt = seededAt },
                new RoleQuota { Role = "ContentAdmin", MaxFavorites = -1,  MaxAquariums = -1, SearchPerDay = -1,  AiQaPerDay = -1, ImageSearchPerDay = -1, UpdatedAt = seededAt },
                new RoleQuota { Role = "SystemAdmin",  MaxFavorites = -1,  MaxAquariums = -1, SearchPerDay = -1,  AiQaPerDay = -1, ImageSearchPerDay = -1, UpdatedAt = seededAt });
        });

        model.Entity<QuotaUsage>(e =>
        {
            e.HasKey(x => new { x.UserId, x.QuotaType, x.Day });
        });
    }
}
