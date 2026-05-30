using AquaHome.Domain.Extensions;
using AquaHome.Domain.Modules;
using AquaHome.EFCore.Data;
using AquaHome.EFCore.Modules;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using FishLover.Shared.Extensions;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(container =>
{
    container.RegisterModule<AquaHomeEFCoreModule>();
    container.RegisterModule<AquaHomeModule>();
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "AquaHome API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new()
    {
        Name          = "Authorization",
        Type          = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme        = "bearer",
        BearerFormat  = "JWT",
        In            = Microsoft.OpenApi.Models.ParameterLocation.Header,
    });
    options.AddSecurityRequirement(new()
    {
        {
            new() { Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } },
            []
        }
    });
});

builder.Services.AddMemoryCache();
builder.Services.AddAquaHomeServices(builder.Configuration);
builder.Services.AddFishLoverJwtAuthentication(builder.Configuration);
builder.Services.AddFishLoverAuthorization();
builder.Services.AddFishLoverTelemetry(builder.Configuration, "AquaHome.API");

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFE", policy =>
        policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty);

var app = builder.Build();

if (app.Configuration.GetValue<bool>("AutoMigrate:OnStartup"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AquaHomeDbContext>();
    await db.Database.MigrateAsync();
    Log.Information("Database migration completed");
}

if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "AquaHome API v1"));
}

app.UseCors("AllowFE");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
app.MapPrometheusScrapingEndpoint();

try
{
    Log.Information("Starting AquaHome.API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
