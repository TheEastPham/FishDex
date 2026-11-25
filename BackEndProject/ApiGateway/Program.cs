using FishLover.Shared.Extensions;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Mỗi môi trường nạp ĐÚNG MỘT file route hoàn chỉnh: ocelot.{Env}.json nếu có, không thì ocelot.json (Production).
// KHÔNG chồng file môi trường lên ocelot.json: cấu hình .NET gộp mảng "Routes" theo vị trí, nên hai file
// khác số/thứ tự route sẽ trộn thuộc tính của nhau (từng làm gateway Dev/Docker không khởi động được:
// "route /aquahome/v1/{everything} has duplicate").
var envOcelotFile = $"ocelot.{builder.Environment.EnvironmentName}.json";
var ocelotFile = File.Exists(Path.Combine(builder.Environment.ContentRootPath, envOcelotFile)) ? envOcelotFile : "ocelot.json";
builder.Configuration.AddJsonFile(ocelotFile, optional: false, reloadOnChange: true);

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
              .AllowAnyHeader()
              // Gateway mới là origin mà trình duyệt nhìn thấy, nên Expose-Headers phải khai ở đây —
              // khai bên FishDex không có tác dụng khi FE đi qua gateway. Đây là hạn mức xem loài
              // của khách chưa đăng nhập (xem AnonSpeciesQuotaFilter bên FishDex.API).
              .WithExposedHeaders(
                  "X-Anon-Views-Limit",
                  "X-Anon-Views-Used",
                  "X-Anon-Views-Remaining",
                  "X-Anon-Views-Reset");
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
// ASP0014 gợi ý map top-level, nhưng endpoint top-level chạy SAU UseOcelot (terminal) → /metrics sẽ mất.
#pragma warning disable ASP0014
app.UseEndpoints(endpoints => endpoints.MapPrometheusScrapingEndpoint());
#pragma warning restore ASP0014

// Use Ocelot middleware
await app.UseOcelot();

app.Run();
