using UserManagement.Domain.Extensions;
using Serilog;
using System.Diagnostics;
using UserManagement.API.Middleware;
using UserManagement.API.Extensions;
using UserManagement.API.Services;
using FishLover.Shared.Extensions;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using UserManagement.Domain.Modules;
using UserManagement.EFCore.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Service Information
const string serviceName = "UserManagement.API";
const string serviceVersion = "1.0.0";

// Configure Autofac as the service provider factory
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

// Configure Autofac container
builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    // Register Autofac modules
    containerBuilder.RegisterModule<UserManagementModule>();
});

// Serilog Configuration
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "UserManagement API", Version = "v1" });
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

// Add UserManagement Domain services (includes EFCore + OpenIddict Core)
builder.Services.AddUserManagementDomain(builder.Configuration);
builder.Services.AddOpenIddictServer(builder.Configuration);
builder.Services.AddHostedService<OpenIddictSeeder>();
builder.Services.AddHostedService<AdminSeeder>();

// OpenTelemetry Configuration
builder.Services.AddFishLoverTelemetry(builder.Configuration, "UserManagement.API");
// JWT Authentication
builder.Services.AddFishLoverJwtAuthentication(builder.Configuration);
builder.Services.AddFishLoverAuthorization();


// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
    // Allow FishDex Swagger to call /connect/* for OAuth2 PKCE flow
    options.AddPolicy("AllowOAuth", policy =>
    {
        policy.WithOrigins("http://localhost:8081", "http://localhost:8080")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Configure Identity cookie to point to our custom login page
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/connect/login";
    options.LogoutPath = "/connect/logout";
});

// Redis (Optional)
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

// Health Checks
builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty)
    .AddRedis(builder.Configuration.GetConnectionString("Redis") ?? string.Empty);

var app = builder.Build();

// Auto-migrate on startup (safe for Docker local dev; idempotent)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UserManagementDbContext>();
    await db.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "UserManagement API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

// Performance logging middleware
app.UseMiddleware<PerformanceLoggingMiddleware>();

app.UseCors("AllowSpecificOrigins");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");

// Prometheus metrics endpoint
app.MapPrometheusScrapingEndpoint();

// Custom Activity Source for manual instrumentation
using var activitySource = new ActivitySource(serviceName);

try
{
    Log.Information("Starting UserManagement.API");
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

public partial class Program {}