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
    public DbSet<AquariumSnapshot>     AquariumSnapshots     => Set<AquariumSnapshot>();
    public DbSet<AquariumSnapshotLike> AquariumSnapshotLikes => Set<AquariumSnapshotLike>();
    public DbSet<Contest>              Contests              => Set<Contest>();
    public DbSet<ContestEntry>         ContestEntries        => Set<ContestEntry>();
    public DbSet<ContestPrizeTier>     ContestPrizeTiers     => Set<ContestPrizeTier>();
    public DbSet<ContestSponsor>       ContestSponsors       => Set<ContestSponsor>();
    public DbSet<Article>              Articles              => Set<Article>();
    public DbSet<ArticleTranslation>   ArticleTranslations   => Set<ArticleTranslation>();
    public DbSet<ArticleAsset>         ArticleAssets         => Set<ArticleAsset>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Aquarium>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.CountryCode).HasMaxLength(8);
            // Worker gom cá theo quốc gia để đẩy sang FishDex — lọc theo cột này.
            e.HasIndex(x => x.CountryCode);
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

        model.Entity<AquariumSnapshot>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Slug).HasMaxLength(150).IsRequired();
            e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.YoutubeVideoUrl).HasMaxLength(500);
            e.Property(x => x.SnapshotData).HasColumnType("jsonb"); // render-only, KHÔNG query/index vào JSON
            e.HasIndex(x => new { x.IsActive, x.WaterType, x.Style, x.AwardTierLevel, x.LikeCount })
             .IsDescending(false, false, false, false, true); // phục vụ gallery filter + sort likes DESC
            e.HasOne(x => x.Aquarium)
             .WithMany()
             .HasForeignKey(x => x.AquariumId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ContestEntry)
             .WithOne()
             .HasForeignKey<AquariumSnapshot>(x => x.ContestEntryId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        model.Entity<AquariumSnapshotLike>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.SnapshotId, x.UserId }).IsUnique();
            e.HasOne(x => x.Snapshot)
             .WithMany()
             .HasForeignKey(x => x.SnapshotId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        model.Entity<Contest>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(150).IsRequired();
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.YouTubePlaylistId).HasMaxLength(100);
        });

        model.Entity<ContestEntry>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.VideoR2Key).HasMaxLength(500);
            e.Property(x => x.YouTubeVideoId).HasMaxLength(20);
            e.Property(x => x.Title).HasMaxLength(100);        // giới hạn title của YouTube
            e.Property(x => x.Description).HasMaxLength(100);  // chốt 100 ký tự cho người dự thi
            e.Property(x => x.RejectionReason).HasMaxLength(500);
            e.HasIndex(x => x.Status);
            // Trang "bài dự thi của tôi" + check nộp trùng đều lọc theo (UserId, ContestId)
            e.HasIndex(x => new { x.UserId, x.ContestId });
            e.HasOne(x => x.Contest)
             .WithMany(c => c.Entries)
             .HasForeignKey(x => x.ContestId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.AquariumSnapshot)
             .WithMany()
             .HasForeignKey(x => x.AquariumSnapshotId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.PrizeTier)
             .WithMany()
             .HasForeignKey(x => x.PrizeTierId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        model.Entity<ContestPrizeTier>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.ImageObjectKey).HasMaxLength(500);
            e.HasIndex(x => new { x.ContestId, x.DisplayOrder });
            e.HasOne(x => x.Contest)
             .WithMany(c => c.PrizeTiers)
             .HasForeignKey(x => x.ContestId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        model.Entity<ContestSponsor>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.Property(x => x.WebsiteUrl).HasMaxLength(500);
            e.Property(x => x.Address).HasMaxLength(300);
            e.Property(x => x.LogoObjectKey).HasMaxLength(500);
            e.HasIndex(x => new { x.ContestId, x.SponsorTier, x.DisplayOrder });
            e.HasOne(x => x.Contest)
             .WithMany(c => c.Sponsors)
             .HasForeignKey(x => x.ContestId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        model.Entity<Article>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Slug).HasMaxLength(160).IsRequired();
            e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.TemplateKey).HasMaxLength(40).IsRequired();
            e.Property(x => x.CoverObjectKey).HasMaxLength(500);
            e.Property(x => x.AuthorName).HasMaxLength(100);
            // Trang list công khai: lọc Published rồi sort PublishedAt DESC — index phủ đúng luồng đó.
            e.HasIndex(x => new { x.Status, x.PublishedAt }).IsDescending(false, true);
            e.HasIndex(x => new { x.Status, x.Type });
            // Postgres text[] + GIN → Tags.Contains(tag) dùng được index thay vì seq scan.
            e.HasIndex(x => x.Tags).HasMethod("gin");
        });

        model.Entity<ArticleTranslation>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Language).HasMaxLength(5).IsRequired();
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.Summary).HasMaxLength(500);
            e.Property(x => x.ContentObjectKey).HasMaxLength(500).IsRequired();
            // Một bài chỉ có một bản dịch cho mỗi ngôn ngữ.
            e.HasIndex(x => new { x.ArticleId, x.Language }).IsUnique();
            e.HasOne(x => x.Article)
             .WithMany(a => a.Translations)
             .HasForeignKey(x => x.ArticleId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        model.Entity<ArticleAsset>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ObjectKey).HasMaxLength(500).IsRequired();
            e.Property(x => x.ContentType).HasMaxLength(50).IsRequired();
            e.Property(x => x.FileName).HasMaxLength(260);
            e.HasIndex(x => x.ArticleId);
            e.HasOne(x => x.Article)
             .WithMany(a => a.Assets)
             .HasForeignKey(x => x.ArticleId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
