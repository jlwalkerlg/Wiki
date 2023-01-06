using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace MultiTenantRouteParams.Database;

public class TenantDbContextFactory
{
    private readonly IConfiguration _configuration;

    public TenantDbContextFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public TenantDbContext Create(Guid tenantId)
    {
        var connectionString = new SqlConnectionStringBuilder
        {
            DataSource = _configuration["Databases:Tenant:DataSource"],
            InitialCatalog = _configuration["Databases:Tenant:InitialCatalogPrefix"] + $".{tenantId}",
            UserID = _configuration["Databases:Tenant:UserID"],
            Password = _configuration["Databases:Tenant:Password"],
            Encrypt = false,
        }.ConnectionString;

        var dbContextOptions = new DbContextOptionsBuilder<TenantDbContext>()
                .UseSqlServer(connectionString)
                .Options;

        return new TenantDbContext(dbContextOptions);
    }
}
