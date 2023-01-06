using Microsoft.EntityFrameworkCore;

namespace MultiTenantRouteParams.Database;

public class CoreDbContext : DbContext
{
    public CoreDbContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new TenantConfiguration());
    }

    public DbSet<Tenant> Tenants { get; set; } = null!;
}
