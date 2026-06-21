using UserManagement.Domain.Extensions;
using Serilog;
using System.Diagnostics;
using UserManagement.API.Middleware;
using UserManagement.API.Extensions;
using UserManagement.API.Services;
using FishLover.Shared.Extensions;
using UserManagement.EFCore.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Service Information
const string serviceName = "UserManagement.API";
const string serviceVersion = "1.0.0";

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
builder.Services.AddMemoryCache();
builder.Services.AddUserManagementDomain(builder.Configuration);
builder.Services.AddOpenIddictServer(builder.Configuration, builder.Environment);
builder.Services.AddHostedService<OpenIddictSeeder>();
builder.Services.AddHostedService<AdminSeeder>();

// OpenTelemetry Configuration
builder.Services.AddFishLoverTelemetry(builder.Configuration, "UserManagement.API");
// JWT Authentication — HS256 symmetric scheme (direct-login tokens)
builder.Services.AddFishLoverJwtAuthentication(builder.Configuration);

// OpenIddict RS256 scheme — validate OAuth2 PKCE tokens issued by this service
// MetadataAddress + JWKS calls đều đi qua internal URL để tránh hairpin NAT issue trên Oracle VM.
// Khi PROD: oidcIssuer = https://api.fishlover.org; oidcInternalUrl = http://localhost:8080
// BackchannelHttpMessageHandler rewrite mọi call tới oidcIssuer → oidcInternalUrl trong nội bộ container.
var oidcIssuer = builder.Configuration["OpenIddict:Issuer"] ?? "http://localhost:8080";
var oidcInternalUrl = builder.Configuration["AuthServer:InternalUrl"] ?? "http://localhost:8080";
builder.Services.AddAuthentication()
    .AddJwtBearer("OpenIddict", options =>
    {
        options.MetadataAddress = $"{oidcInternalUrl.TrimEnd('/')}/.well-known/openid-configuration";
        options.RequireHttpsMetadata = false;
        // Rewrite external issuer URL → internal URL so JWKS fetch stays in-container (no hairpin NAT needed)
        var externalBase = oidcIssuer.TrimEnd('/');
        var internalBase = oidcInternalUrl.TrimEnd('/');
        if (!string.Equals(externalBase, internalBase, StringComparison.OrdinalIgnoreCase))
        {
            options.BackchannelHttpHandler = new UserManagement.API.Infrastructure.InternalUrlRewriteHandler(externalBase, internalBase);
        }
        var issuer = externalBase;
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidIssuers = [issuer, issuer + "/"],
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
        };
    });

builder.Services.AddFishLoverAuthorization();

// Override DefaultPolicy: accept cả Bearer (HS256) lẫn OpenIddict (RS256)
builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder(
        Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme,
        "OpenIddict")
        .RequireAuthenticatedUser()
        .Build();
});


// CORS — origins đọc từ config; local dev set trong appsettings.Development.json,
// production set qua env var AllowedOrigins__0, __1, ... trong docker-compose
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
    // AllowOAuth applied on AuthorizationController (/connect/*) for OAuth2 PKCE flow
    options.AddPolicy("AllowOAuth", policy =>
    {
        policy.WithOrigins(allowedOrigins)
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
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty)
    .AddRedis(builder.Configuration.GetConnectionString("Redis") ?? string.Empty);

var app = builder.Build();

// Auto-migrate: chỉ chạy khi AutoMigrate:OnStartup=true (local/Docker dev)
// Production nên chạy migration qua CI/CD pipeline thay vì khi startup
if (app.Configuration.GetValue<bool>("AutoMigrate:OnStartup"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<UserManagementDbContext>();
    await db.Database.MigrateAsync();
    Log.Information("Database migration completed");
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

app.UseMiddleware<PerformanceLoggingMiddleware>();
app.UseMiddleware<OpenIddictExceptionMiddleware>();
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