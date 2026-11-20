using FishDex.EFCore.DbContexts;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace FishDex.Tests.Infrastructure;

/// <summary>
/// Một container Postgres cho cả collection: start + migrate một lần, mỗi test gọi
/// <see cref="ResetAsync"/> để bắt đầu từ DB rỗng. Test trong cùng collection chạy tuần tự.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    // Cùng image với Pipeline/local/FishDex — migration có bật extension (pg_trgm, vector).
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("pgvector/pgvector:pg16")
        .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var db = CreateDb();
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync()
    {
        return _container.DisposeAsync().AsTask();
    }

    public FishDexDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<FishDexDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;
        return new FishDexDbContext(options);
    }

    public async Task ResetAsync()
    {
        await using var db = CreateDb();
        var tables = db.Model.GetEntityTypes()
            .Select(t => t.GetTableName())
            .Where(n => n != null)
            .Distinct()
            .Select(n => $"\"{n}\"");
        // Tên bảng lấy từ EF model, không phải input — an toàn để ghép.
        var sql = "TRUNCATE TABLE " + string.Join(", ", tables) + " RESTART IDENTITY CASCADE;";
        await db.Database.ExecuteSqlRawAsync(sql);
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
