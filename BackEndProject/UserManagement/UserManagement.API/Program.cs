using UserManagement.Domain.Extensions;
using Serilog;
using System.Diagnostics;
using UserManagement.API.Middleware;
using FishLover.Shared.Extensions;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using UserManagement.Domain.Modules;

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
builder.Services.AddSwaggerGen();

// Add UserManagement Domain services (includes EFCore)
builder.Services.AddUserManagementDomain(builder.Configuration);

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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
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