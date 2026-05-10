using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace FishLover.Shared.Extensions;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddFishLoverTelemetry(this IServiceCollection services, 
        IConfiguration configuration, 
        string serviceName)
    {
        // Tự lấy version từ Assembly cho chuyên nghiệp
        var serviceVersion = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0.0";

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName, serviceVersion)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["environment.name"] = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Development",
                    ["service.instance.id"] = Environment.MachineName
                }))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.Filter = httpContext => 
                        !httpContext.Request.Path.StartsWithSegments("/health") &&
                        !httpContext.Request.Path.StartsWithSegments("/metrics");
                })
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation(options =>
                {
                    options.SetDbStatementForText = true;
                })
                .AddRedisInstrumentation()
                .AddSource(serviceName)
                // Export ra OTLP thay vì Console để sau này dùng Grafana
                .AddOtlpExporter(opt => {
                    opt.Endpoint = new Uri(configuration["OpenTelemetry:ExportEndpoint"] ?? "http://localhost:4317");
                }))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddPrometheusExporter());

        return services;
    }
}