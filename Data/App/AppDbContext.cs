using Microsoft.EntityFrameworkCore;
using ThryftAiServer.Models;

namespace ThryftAiServer.Data.App;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<FashionProduct> FashionProducts { get; set; }
    public DbSet<Purchase> Purchases { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<FashionProduct>(entity =>
        {
            entity.HasIndex(e => e.ExternalId).IsUnique();
        });
    }
}
