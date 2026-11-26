using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using UserManagement.Domain.Services.Interfaces;

namespace UserManagement.Tests.Infrastructure;

/// <summary>
/// Một Postgres (Testcontainers) + một UserManagement.API trong bộ nhớ cho cả collection.
/// Mỗi test tự dùng email riêng nên không cần dọn DB giữa các test.
/// </summary>
public sealed class UserManagementApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder("pgvector/pgvector:pg16").Build();
    private readonly string _keyDir = Path.Combine(Path.GetTempPath(), $"um-tests-openiddict-{Guid.NewGuid():N}");
    private WebApplicationFactory<Program>? _factory;

    public FakeEmailService Emails { get; } = new();

    public async Task InitializeAsync()
    {
        await _db.StartAsync();

        // Program.cs đọc cấu hình ngay lúc đăng ký service (connection string, RequireInvitation...),
        // nên phải đặt qua biến môi trường TRƯỚC khi host được dựng — biến môi trường đè appsettings.json.
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", _db.GetConnectionString());
        Environment.SetEnvironmentVariable("AutoMigrate__OnStartup", "true");
        Environment.SetEnvironmentVariable("RequireInvitation", "false");
        Environment.SetEnvironmentVariable("OpenIddict__KeyDir", _keyDir);

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IEmailService>();
                services.AddSingleton<IEmailService>(Emails);
            });
        });

        // Dựng host ngay (migrate + seed OpenIddict) để lỗi khởi động lộ ra ở đây, không lẫn vào test đầu tiên.
        _ = _factory.Server;
    }

    public HttpClient CreateClient() =>
        _factory!.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    public async Task DisposeAsync()
    {
        if (_factory is not null) await _factory.DisposeAsync();
        await _db.DisposeAsync();
        if (Directory.Exists(_keyDir)) Directory.Delete(_keyDir, recursive: true);
    }
}

[CollectionDefinition(Name)]
public sealed class UserManagementApiCollection : ICollectionFixture<UserManagementApiFixture>
{
    public const string Name = "usermanagement-api";
}
