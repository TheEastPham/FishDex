using FishDex.EFCore.Entity.Ecologies;
using FishDex.EFCore.Entity.Media;
using FishDex.EFCore.Entity.MorphData;
using FishDex.EFCore.Entity.Occurrence;
using FishDex.EFCore.Entity.Species;
using FishDex.EFCore.Entity.Stocks;
using Microsoft.EntityFrameworkCore;

namespace FishDex.EFCore.DbContexts;

public class FishDexDbContext : DbContext
{
    public FishDexDbContext(DbContextOptions<FishDexDbContext> options) : base(options)
    {
    }

    // Species
    public DbSet<Family>  Families { get; set; }
    public DbSet<Genus>   Genuses  { get; set; }
    public DbSet<Species> Species  { get; set; }

    // Stocks
    public DbSet<Stock>                 Stocks                 { get; set; }
    public DbSet<StockConservation>     StockConservations     { get; set; }
    public DbSet<StockEnvironment>      StockEnvironments      { get; set; }
    public DbSet<StockExternalRef>      StockExternalRefs      { get; set; }
    public DbSet<StockDataAvailability> StockDataAvailabilities{ get; set; }
    public DbSet<StockMetadata>         StockMetadatas         { get; set; }

    // MorphData
    public DbSet<MorphData>        MorphData        { get; set; }
    public DbSet<MorphTeeth>       MorphTeeth       { get; set; }
    public DbSet<MorphPigmentation>MorphPigmentations{ get; set; }
    public DbSet<MorphFins>        MorphFins        { get; set; }
    public DbSet<MorphMeristics>   MorphMeristics   { get; set; }
    public DbSet<MorphMetrics>     MorphMetrics     { get; set; }

    // Ecologies
    public DbSet<Ecology>          Ecologies        { get; set; }
    public DbSet<HabitatZone>      HabitatZones     { get; set; }
    public DbSet<FeedingAndDiet>   FeedingAndDiets  { get; set; }
    public DbSet<Associations>     Associations     { get; set; }
    public DbSet<Substrate>        Substrates       { get; set; }
    public DbSet<SpecialHabitat>   SpecialHabitats  { get; set; }
    public DbSet<CircadianBehavior>CircadianBehaviors{ get; set; }

    // Other
    public DbSet<Occurrence>  Occurrences  { get; set; }
    public DbSet<SystemImage> SystemImages { get; set; }
    // NOTE: Ecosystem bị xoá khỏi DbSet vì entity rỗng.
    //       Thêm lại khi entity có đủ properties.

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Family ──────────────────────────────────────────────
        // PK: Guid Id (dùng cho S3 folder path + URL obfuscation)
        // FamCode: unique int index để JOIN với FishBase data
        modelBuilder.Entity<Family>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.FamCode).IsUnique();

            // FIX: khai báo FK rõ ràng thay vì shadow property
            // Genus cần có property FamId (Guid) — xem note bên dưới
            entity.HasMany(e => e.Genuses)
                .WithOne()
                .HasForeignKey("FamId")       // shadow FK nếu Genus chưa có prop
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Genus ────────────────────────────────────────────────
        modelBuilder.Entity<Genus>(entity =>
        {
            entity.HasKey(e => e.GenusCode);

            entity.HasMany(e => e.Species)
                .WithOne(s => s.Genus)
                .HasForeignKey(s => s.GenusCode)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Species ──────────────────────────────────────────────
        // PK: Guid Id
        // SpecCode: alternate key (unique index) dùng làm FK target cho Stock, Ecology, Occurrence
        modelBuilder.Entity<Species>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SpecCode).IsUnique();

            entity.HasOne(e => e.Family)
                .WithMany()
                .HasForeignKey(e => e.FamId)
                .OnDelete(DeleteBehavior.Restrict);

            // Stock -> Species via SpecCode (alternate key)
            entity.HasMany(e => e.Stocks)
                .WithOne()
                .HasForeignKey(s => s.SpecCode)
                .HasPrincipalKey(e => e.SpecCode)
                .OnDelete(DeleteBehavior.Restrict);

            // SystemImage -> Species via SpecCode
            entity.HasMany(e => e.Pictures)
                .WithOne()
                .HasForeignKey(p => p.SpecCode)
                .HasPrincipalKey(e => e.SpecCode)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Stock ────────────────────────────────────────────────
        modelBuilder.Entity<Stock>(entity =>
        {
            entity.HasKey(e => e.StockCode);

            entity.HasOne(e => e.Conservation)
                .WithOne(c => c.Stock)
                .HasForeignKey<StockConservation>(c => c.StockCode)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Environment)
                .WithOne(e => e.Stock)
                .HasForeignKey<StockEnvironment>(e => e.StockCode)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ExternalRefs)
                .WithOne(e => e.Stock)
                .HasForeignKey<StockExternalRef>(e => e.StockCode)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.DataFlags)
                .WithOne(d => d.Stock)
                .HasForeignKey<StockDataAvailability>(d => d.StockCode)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Metadata)
                .WithOne(m => m.Stock)
                .HasForeignKey<StockMetadata>(m => m.StockCode)
                .OnDelete(DeleteBehavior.Cascade);

            // FIX: Stock -> MorphData (1-1) — bị thiếu trong bản gốc
            entity.HasOne<MorphData>()
                .WithOne()
                .HasForeignKey<MorphData>(m => m.StockCode)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── MorphData ────────────────────────────────────────────
        modelBuilder.Entity<MorphData>(entity =>
        {
            entity.HasKey(e => e.StockCode);

            entity.HasOne(e => e.Teeth)
                .WithOne(t => t.MorphData)
                .HasForeignKey<MorphTeeth>(t => t.StockCode)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Pigmentation)
                .WithOne(p => p.MorphData)
                .HasForeignKey<MorphPigmentation>(p => p.StockCode)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Fins)
                .WithOne(f => f.MorphData)
                .HasForeignKey<MorphFins>(f => f.StockCode)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Meristics)
                .WithOne(m => m.MorphData)
                .HasForeignKey<MorphMeristics>(m => m.StockCode)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Metrics)
                .WithOne(m => m.MorphData)
                .HasForeignKey<MorphMetrics>(m => m.StockCode)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Ecology ──────────────────────────────────────────────
        // FIX: wire Ecology -> Species via SpecCode (alternate key)
        modelBuilder.Entity<Ecology>(entity =>
        {
            entity.HasKey(e => e.EcologyId);

            // Ecology thuộc về Species qua SpecCode
            entity.HasIndex(e => e.SpecCode);  // index để query nhanh

            entity.HasOne(e => e.HabitatZone)
                .WithOne(h => h.Ecology)
                .HasForeignKey<HabitatZone>(h => h.EcologyId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.FeedingAndDiet)
                .WithOne(f => f.Ecology)
                .HasForeignKey<FeedingAndDiet>(f => f.EcologyId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Associations)
                .WithOne(a => a.Ecology)
                .HasForeignKey<Associations>(a => a.EcologyId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Substrate)
                .WithOne(s => s.Ecology)
                .HasForeignKey<Substrate>(s => s.EcologyId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.SpecialHabitat)
                .WithOne(s => s.Ecology)
                .HasForeignKey<SpecialHabitat>(s => s.EcologyId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.CircadianBehavior)
                .WithOne(c => c.Ecology)
                .HasForeignKey<CircadianBehavior>(c => c.EcologyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Occurrence ───────────────────────────────────────────
        modelBuilder.Entity<Occurrence>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SpecCode);  // query by species thường xuyên
        });

        // ── SystemImage ──────────────────────────────────────────
        modelBuilder.Entity<SystemImage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SpecCode);
        });

        // ── StockDataAvailability: bool? thay vì string ──────────
        // EF Core tự handle bool? mapping — không cần config thêm
        // Nhớ đổi property type trong entity từ string -> bool?
    }
}
