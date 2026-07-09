using AquaHome.Domain.Extensions;
using AquaHome.EFCore.Data;
using AquaHome.EFCore.Extensions;
using FishLover.Shared.Extensions;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers(options =>
{
    options.Filters.Add<AquaHome.API.Filters.QuotaExceededExceptionFilter>(); // quota vượt → 429
    options.Filters.Add<AquaHome.API.Filters.ContestValidationExceptionFilter>(); // video contest invalid → 422
    options.Filters.Add<AquaHome.API.Filters.StorageOverloadedExceptionFilter>(); // R2 staging quá tải → 503
});
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
builder.Services.AddAquaHomeRepositories();
builder.Services.AddAquaHomeDomainServices(builder.Configuration);
builder.Services.AddAquaHomeStorage(builder.Configuration);
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
            ValidIssuers = [issuer, issuer + "/"],
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            // Token OpenIddict để role ở claim "role" — map để RequireRole("SystemAdmin") khớp
            RoleClaimType = "role",
            NameClaimType = "name",
        };
    });

builder.Services.AddFishLoverAuthorization();
// [Authorize] + policy admin đều phải chấp nhận cả Bearer (direct login) lẫn OpenIddict (OAuth2 PKCE).
// Policy shared RequireSystemAdmin/RequireContentAdmin KHÔNG khai báo scheme → chỉ dùng default (Bearer)
// → token OpenIddict bị 401 (FE hiểu là hết phiên → logout). Đăng ký đè ở đây với cả 2 scheme.
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
builder.Services.AddFishLoverTelemetry(builder.Configuration, "aquahome");

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
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty);

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
