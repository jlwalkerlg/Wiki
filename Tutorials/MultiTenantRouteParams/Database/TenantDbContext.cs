using Microsoft.EntityFrameworkCore;

namespace MultiTenantRouteParams.Database;

public class TenantDbContext : DbContext
{
    public TenantDbContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new TenantDataConfiguration());
    }

    public DbSet<TenantData> TenantData { get; set; } = null!;
}
