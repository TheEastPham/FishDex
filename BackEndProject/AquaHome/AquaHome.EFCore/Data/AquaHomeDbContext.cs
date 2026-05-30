using AquaHome.EFCore.Entity;
using Microsoft.EntityFrameworkCore;

namespace AquaHome.EFCore.Data;

public class AquaHomeDbContext(DbContextOptions<AquaHomeDbContext> options) : DbContext(options)
{
    public DbSet<Aquarium>     Aquariums     => Set<Aquarium>();
    public DbSet<AquariumFish> AquariumFish  => Set<AquariumFish>();
    public DbSet<UserFavorite> UserFavorites => Set<UserFavorite>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Aquarium>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Type).HasMaxLength(20);
            e.Property(x => x.Description).HasMaxLength(500);
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
    }
}
