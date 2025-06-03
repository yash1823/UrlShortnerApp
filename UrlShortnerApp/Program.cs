using AspNetCoreRateLimit;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Data;
using UrlShortnerApp.Middleware;
using UrlShortnerApp.Models;

var builder = WebApplication.CreateBuilder(args);

// ===== DATABASE SETUP =====
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ===== IN-MEMORY RATE LIMITING =====
builder.Services.AddMemoryCache();
var check = builder.Configuration.GetSection("UrlOptions") ?? throw new MissingFieldException("UrlOptions section is missing in configuration.");
builder.Services.Configure<UrlOptions>(check);
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