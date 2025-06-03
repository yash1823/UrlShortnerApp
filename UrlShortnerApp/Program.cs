using AspNetCoreRateLimit;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Data;
using UrlShortnerApp.Middleware;
using UrlShortnerApp.Models;

var builder = WebApplication.CreateBuilder(args);

// ===== ENVIRONMENT VARIABLE FALLBACK FOR DB CONNECTION =====
var dbConnectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(dbConnectionString))
    throw new InvalidOperationException("Database connection string not found. Set 'DB_CONNECTION_STRING' env variable or provide in appsettings.");

// ===== DATABASE SETUP =====
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(dbConnectionString));

// ===== URL BASE (OPTIONAL) =====
var urlOptionsSection = builder.Configuration.GetSection("UrlOptions");
var baseUrlFromEnv = Environment.GetEnvironmentVariable("BASE_URL");
if (!string.IsNullOrEmpty(baseUrlFromEnv))
    urlOptionsSection["BaseUrl"] = baseUrlFromEnv; // override if env var is set

builder.Services.Configure<UrlOptions>(urlOptionsSection);

// ===== IN-MEMORY RATE LIMITING =====
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();

// ===== SERVICES =====
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ====== PORT BINDING FOR RENDER ======
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

// ===== MIGRATE DATABASE =====
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// ===== MIDDLEWARE =====
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseIpRateLimiting();
app.UseHttpsRedirection();
app.UseMiddleware<GlobalExceptionHandler>();
app.MapGet("/{shortCode}", async (string shortCode, AppDbContext db) =>
{
    var url = await db.Urls
        .AsNoTracking()
        .FirstOrDefaultAsync(u => u.ShortCode == shortCode);

    return url is null
        ? Results.NotFound()
        : Results.Redirect(url.OriginalUrl);
});
app.MapControllers();

app.Run();