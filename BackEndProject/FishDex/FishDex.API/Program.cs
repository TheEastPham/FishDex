using Autofac;
using Autofac.Extensions.DependencyInjection;
using FishDex.Domain.Modules;
using FishDex.Domain.Settings;
using FishDex.EFCore.Extensions;
using FishDex.EFCore.Modules;
using FishLover.Shared.Extensions;
using Serilog;
using Serilog.Events;

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

    // ── Autofac (giống UserManagement) ────────────────────────
    builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
    builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
    {
        containerBuilder.RegisterModule<FishDexEFCoreModule>();
        containerBuilder.RegisterModule<FishDexModule>();
    });

    // ── Database — PostgreSQL ──────────────────────────────────
    builder.Services.AddFishDexDatabase(builder.Configuration);
    builder.Services.AddMemoryCache();
    builder.Services.Configure<FishDexSettings>(
        builder.Configuration.GetSection(FishDexSettings.SectionName));

    // OpenTelemetry Configuration
    builder.Services.AddFishLoverTelemetry(builder.Configuration, "FishDex.API");

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
            var issuer = authServerPublicUrl.TrimEnd('/');
            options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                // Accept cả "http://host" lẫn "http://host/" — OpenIddict thêm trailing slash
                ValidIssuers = [issuer, issuer + "/"],
                ValidateIssuer = true,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
            };
        });

    builder.Services.AddFishLoverAuthorization();
    // [Authorize] chấp nhận cả Bearer (direct login) lẫn OpenIddict (OAuth2 PKCE)
    builder.Services.AddAuthorization(options =>
    {
        options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder(
            Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme,
            "OpenIddict")
            .RequireAuthenticatedUser()
            .Build();
    });


    // ── Controllers + OpenAPI ──────────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    var authServerUrl = builder.Configuration["AuthServer:Url"] ?? "http://localhost:8080";
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
                .AllowAnyMethod();
        });
    });

    // ── HealthChecks ──────────────────────────────────────────
    builder.Services.AddHealthChecks()
        .AddNpgSql(
            builder.Configuration.GetConnectionString("FishDexDb")!,
            name: "fishdex-db",
            tags: ["db", "postgres"]);

    var app = builder.Build();

    // ── Middleware pipeline ────────────────────────────────────
    if (!app.Environment.IsProduction())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "FishDex API v1");
            options.RoutePrefix = "swagger";
            options.OAuthClientId("FishDex_Swagger");
            options.OAuthUsePkce();
            options.OAuthScopes("openid", "profile", "email", "roles", "fishdex");
        });
    }

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