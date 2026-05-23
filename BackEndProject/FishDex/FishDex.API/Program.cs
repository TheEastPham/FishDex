using Autofac;
using Autofac.Extensions.DependencyInjection;
using FishDex.EFCore.Extensions;
using FishLover.Shared.Extensions; // JwtAuthenticationExtensions, OpenTelemetryExtensions
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
        // TODO: thêm FishDexEFCoreModule khi có Domain layer
        // containerBuilder.RegisterModule<FishDexEFCoreModule>();
        // containerBuilder.RegisterModule<FishDexDomainModule>();
    });

    // ── Database — PostgreSQL ──────────────────────────────────
    builder.Services.AddFishDexDatabase(builder.Configuration);

    // OpenTelemetry Configuration
    builder.Services.AddFishLoverTelemetry(builder.Configuration, "FishDex.API");
// JWT Authentication
    builder.Services.AddFishLoverJwtAuthentication(builder.Configuration);
    builder.Services.AddFishLoverAuthorization();


    // ── Controllers + OpenAPI ──────────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new() { Title = "FishDex API", Version = "v1" });
        options.AddSecurityDefinition("Bearer", new()
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        });
        options.AddSecurityRequirement(new()
        {
            {
                new() { Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } },
                []
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
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "FishDex API v1");
            options.RoutePrefix = "swagger";
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