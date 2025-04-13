using Microsoft.EntityFrameworkCore;
using UrlShortener.Models;

namespace UrlShortener.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Url> Urls { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Url>()
            .HasIndex(u => u.ShortCode)
            .IsUnique();
    }
}