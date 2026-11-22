using ActivityMonitor.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ActivityMonitor.Api.Data;

public class ActivityDbContext : DbContext
{
    public ActivityDbContext(DbContextOptions<ActivityDbContext> options)
        : base(options)
    {
    }

    public DbSet<ActivityEvent> Events => Set<ActivityEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActivityEvent>(entity =>
        {
            entity.ToTable("Events");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserEmail).HasMaxLength(256).IsRequired();
            entity.Property(e => e.ProductName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Action).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => new { e.TenantId, e.OccurredAt });
        });
    }
}
