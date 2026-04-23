using Microsoft.EntityFrameworkCore;
using UrlShortener.Models;

namespace UrlShortener.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ShortUrl> Urls => Set<ShortUrl>();
    public DbSet<ShortUrlRule> UrlRules => Set<ShortUrlRule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ShortUrl>()
            .HasIndex(url => url.ShortCode)
            .IsUnique();

        modelBuilder.Entity<ShortUrlRule>()
            .HasOne(rule => rule.ShortUrl)
            .WithMany(url => url.Rules)
            .HasForeignKey(rule => rule.ShortUrlId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ShortUrlRule>()
            .HasIndex(rule => new { rule.ShortUrlId, rule.Priority, rule.Id });

        base.OnModelCreating(modelBuilder);
    }
}
