using FishDex.Domain.SpeciesProfiles;
using FishDex.EFCore.DbContexts;
using FishDex.EFCore.Repository;
using FishDex.Tests.Infrastructure;

namespace FishDex.Tests.SpeciesProfiles;

/// <summary>
/// Mỗi test bắt đầu từ DB rỗng. Seed qua <see cref="Seed"/> trên một DbContext, đọc qua
/// <see cref="ISpeciesProfile"/> trên DbContext khác — để không vô tình đọc entity đang track.
/// </summary>
public abstract class SpeciesProfileTestBase(PostgresFixture pg) : IAsyncLifetime
{
    protected PostgresFixture Pg { get; } = pg;
    protected FishDexDbContext Db { get; private set; } = null!;
    protected Seed Seed { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await Pg.ResetAsync();
        Db   = Pg.CreateDb();
        Seed = new Seed(Db);
    }

    public async Task DisposeAsync()
    {
        await Db.DisposeAsync();
    }

    protected ISpeciesProfile NewProfile(out FishDexDbContext db, bool storageConfigured = true)
    {
        db = Pg.CreateDb();
        return new SpeciesProfile(db, new CommunitySpeciesRepository(db), new FakeStorage(storageConfigured));
    }

    protected ISpeciesProfile NewProfile(bool storageConfigured = true)
    {
        return NewProfile(out _, storageConfigured);
    }
}
