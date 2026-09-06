using FishDex.API.Filters;
using FishDex.Domain.Extensions;
using FishDex.Domain.Services;
using FishDex.Domain.Services.Interfaces;
using FishDex.Domain.Settings;
using FishDex.EFCore.DbContexts;
using FishDex.EFCore.Extensions;
using FishLover.Shared.Extensions;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using StackExchange.Redis;

// ── Bootstrap logger (trước khi host khởi động) ───────────────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting FishDex.API");

    var builder = WebApplication.CreateBuilder(args);

    // appsettings.Local.json — gitignored, dùng cho local credentials (R2, secrets)
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

    // ── Serilog (giống UserManagement) ────────────────────────
    builder.Host.UseSerilog((ctx, services, config) => config
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File(
            path: "logs/fishdex-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7));

    // ── Database — PostgreSQL ──────────────────────────────────
    builder.Services.AddFishDexDatabase(builder.Configuration);
    builder.Services.AddFishDexRepositories();
    builder.Services.AddFishDexServices();
    builder.Services.AddMemoryCache(opts => opts.SizeLimit = 3_000);
    builder.Services.Configure<FishDexSettings>(
        builder.Configuration.GetSection(FishDexSettings.SectionName));
    builder.Services.Configure<StorageSettings>(
        builder.Configuration.GetSection(StorageSettings.SectionName));

    // ── Hạn mức xem loài cho khách chưa đăng nhập ─────────────
    // Redis dùng chung với UserManagement/AquaHome trên VM1. Không cấu hình chuỗi kết nối
    // (local dev) thì AnonQuotaService tự cho qua tất cả — không phải dựng Redis mới chạy được API.
    builder.Services.Configure<AnonQuotaSettings>(
        builder.Configuration.GetSection(AnonQuotaSettings.SectionName));

    var redisConnection = builder.Configuration.GetConnectionString("Redis");
    if (!string.IsNullOrWhiteSpace(redisConnection))
    {
        builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var config = ConfigurationOptions.Parse(redisConnection);
            // Redis chết lúc API khởi động thì API vẫn phải lên — hạn mức fail-open, không chặn ai.
            config.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(config);
        });
    }

    builder.Services.AddScoped<IAnonQuotaService>(sp => new AnonQuotaService(
        sp.GetService<IConnectionMultiplexer>(),
        sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AnonQuotaSettings>>(),
        sp.GetRequiredService<ILogger<AnonQuotaService>>()));
    builder.Services.AddScoped<AnonSpeciesQuotaFilter>();

    // OpenTelemetry Configuration
    builder.Services.AddFishLoverTelemetry(builder.Configuration, "fishdex");

    // JWT Authentication — symmetric scheme (direct-login tokens từ UserManagement)
    builder.Services.AddFishLoverJwtAuthentication(builder.Configuration);

    // OAuth2 PKCE scheme — validate OpenIddict-issued tokens qua JWKS discovery
    var authServerPublicUrl = builder.Configuration["AuthServer:Url"] ?? "http://localhost:8080";
    var authServerInternalUrl = builder.Configuration["AuthServer:Authority"] ?? authServerPublicUrl;
    builder.Services.AddAuthentication()
        .AddJwtBearer("OpenIddict", options =>
        {
            options.MetadataAddress = $"{authServerInternalUrl}/.well-known/openid-configuration";
            options.RequireHttpsMetadata = false;
            // Tắt legacy inbound claim mapping — mặc định .NET đổi "role" → URI dài khiến
            // RoleClaimType="role" không khớp claim thực tế → RequireRole luôn fail (giống fix ở AquaHome).
            options.MapInboundClaims = false;
            var issuer = authServerPublicUrl.TrimEnd('/');
            options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                // Accept cả "http://host" lẫn "http://host/" — OpenIddict thêm trailing slash
                ValidIssuers = [issuer, issuer + "/"],
                ValidateIssuer = true,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                // Token OpenIddict để role ở claim "role" — map để RequireRole khớp
                RoleClaimType = "role",
                NameClaimType = "name",
            };
        });

    builder.Services.AddFishLoverAuthorization();
    // [Authorize] + policy admin đều phải chấp nhận cả Bearer (direct login) lẫn OpenIddict (OAuth2 PKCE).
    // Policy shared RequireContentAdmin/RequireSystemAdmin KHÔNG khai scheme → chỉ default → token
    // OpenIddict bị 401. Đăng ký đè ở đây với cả 2 scheme (giống AquaHome).
    builder.Services.AddAuthorization(options =>
    {
        string[] bothSchemes =
        [
            Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme,
            "OpenIddict",
        ];

        options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder(bothSchemes)
            .RequireAuthenticatedUser()
            .Build();

        options.AddPolicy("RequireSystemAdmin", p => p
            .AddAuthenticationSchemes(bothSchemes)
            .RequireRole("SystemAdmin"));

        options.AddPolicy("RequireContentAdmin", p => p
            .AddAuthenticationSchemes(bothSchemes)
            .RequireRole("SystemAdmin", "ContentAdmin"));
    });


    // ── Controllers + OpenAPI ──────────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    // SwaggerUrl: trực tiếp tới UserManagement (bypass gateway) để tránh CORS từ Swagger origin
    var authServerUrl = builder.Configuration["AuthServer:SwaggerUrl"]
        ?? builder.Configuration["AuthServer:Url"]
        ?? "http://localhost:8080";
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new() { Title = "FishDex API", Version = "v1" });

        // Bearer — manual paste for quick testing
        options.AddSecurityDefinition("Bearer", new()
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        });

        // OAuth2 — Authorization Code + PKCE via UserManagement
        options.AddSecurityDefinition("OAuth2", new()
        {
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.OAuth2,
            Flows = new()
            {
                AuthorizationCode = new()
                {
                    AuthorizationUrl = new Uri($"{authServerUrl}/connect/authorize"),
                    TokenUrl = new Uri($"{authServerUrl}/connect/token"),
                    Scopes = new Dictionary<string, string>
                    {
                        ["openid"] = "OpenID Connect",
                        ["profile"] = "Profile",
                        ["email"] = "Email",
                        ["roles"] = "Roles",
                        ["fishdex"] = "FishDex API"
                    }
                }
            }
        });

        options.AddSecurityRequirement(new()
        {
            {
                new() { Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } },
                []
            }
        });
        options.AddSecurityRequirement(new()
        {
            {
                new()
                {
                    Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "OAuth2" }
                },
                ["openid", "profile", "email", "roles", "fishdex"]
            }
        });
    });

    // ── CORS (cho FE gọi trực tiếp nếu không qua Gateway) ─────
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("FishDexCors", policy =>
        {
            var allowedOrigins = builder.Configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>() ?? [];

            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                // Không expose thì trình duyệt giấu header custom khỏi JS và FE không đọc nổi
                // số lượt còn lại của chính nó.
                .WithExposedHeaders(AnonSpeciesQuotaFilter.ResponseHeaders);
        });
    });

    // ── HealthChecks ──────────────────────────────────────────
    builder.Services.AddHealthChecks()
        .AddNpgSql(
            builder.Configuration.GetConnectionString("FishDexDb")!,
            name: "fishdex-db",
            tags: ["db", "postgres"]);

    var app = builder.Build();

    // ── Auto-migrate — chỉ chạy khi AutoMigrate:OnStartup=true (Docker/Dev) ──
    if (app.Configuration.GetValue<bool>("AutoMigrate:OnStartup"))
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FishDexDbContext>();
        await db.Database.MigrateAsync();
        Log.Information("FishDex database migration completed");
    }

    // ── Middleware pipeline ────────────────────────────────────
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "FishDex API v1");
        options.RoutePrefix = "swagger";
        options.OAuthClientId("FishDex_Swagger");
        options.OAuthUsePkce();
        options.OAuthScopes("openid", "profile", "email", "roles", "fishdex");
    });

    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    });

    app.UseHttpsRedirection();
    app.UseCors("FishDexCors");
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHealthChecks("/health");
    // Prometheus VM2 scrape qua private IP 10.0.0.64:8081/metrics.
    // Metrics đã được cấu hình sẵn trong AddFishLoverTelemetry, chỉ thiếu endpoint này.
    app.MapPrometheusScrapingEndpoint();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "FishDex.API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}