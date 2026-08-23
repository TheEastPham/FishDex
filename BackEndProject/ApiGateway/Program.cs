using FishLover.Shared.Extensions;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add Ocelot configuration
builder.Configuration
    .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"ocelot.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

// Add services to the container.
builder.Services.AddOcelot();

// ADD JWT AUTHENTICATION HERE
builder.Services.AddFishLoverJwtAuthentication(builder.Configuration);

// OpenTelemetry — trace chặng gateway + propagate traceparent sang downstream services
builder.Services.AddFishLoverTelemetry(builder.Configuration, "gateway");

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var raw = Environment.GetEnvironmentVariable("FE_ORIGIN");
        var origins = string.IsNullOrEmpty(raw)
            ? ["http://localhost:5173", "http://localhost:3000"]
            : raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        policy.WithOrigins(origins)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add controllers for health check
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();
app.UseCors();

// /metrics phải map TRƯỚC Ocelot. UseOcelot là middleware terminal: nếu để sau, nó sẽ coi
// /metrics là một request cần route xuống downstream và trả UnableToFindDownstreamRouteError.
// Endpoint này KHÔNG được ra internet — nginx chặn /metrics, Prometheus VM2 scrape trực tiếp
// qua private IP 10.0.0.64:5000.
app.UseEndpoints(endpoints => endpoints.MapPrometheusScrapingEndpoint());

// Use Ocelot middleware
await app.UseOcelot();

app.Run();
