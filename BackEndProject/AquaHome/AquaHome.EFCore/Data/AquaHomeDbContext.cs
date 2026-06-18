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
    }
}
