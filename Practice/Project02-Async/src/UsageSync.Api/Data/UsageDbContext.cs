using Microsoft.EntityFrameworkCore;
using UsageSync.Api.Models;

namespace UsageSync.Api.Data;

public class UsageDbContext : DbContext
{
    public UsageDbContext(DbContextOptions<UsageDbContext> options)
        : base(options)
    {
    }

    public DbSet<UsageRecord> UsageRecords => Set<UsageRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UsageRecord>(entity =>
        {
            entity.ToTable("UsageRecords");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.VendorId).HasMaxLength(50).IsRequired();
            entity.Property(r => r.UserEmail).HasMaxLength(256).IsRequired();
            entity.Property(r => r.Product).HasMaxLength(200).IsRequired();
            entity.HasIndex(r => new { r.TenantId, r.UsageDateUtc });
            entity.HasIndex(r => new { r.VendorId, r.UsageDateUtc });
        });
    }
}
