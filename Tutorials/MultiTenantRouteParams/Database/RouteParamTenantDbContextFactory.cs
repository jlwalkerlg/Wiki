using MultiTenantRouteParams.Routing;

namespace MultiTenantRouteParams.Database;

public class RouteParamTenantDbContextFactory
{
    private readonly TenantDbContextFactory _tenantDbContextFactory;
    private readonly TenantIdAccessor _tenantIdAccessor;

    public RouteParamTenantDbContextFactory(TenantDbContextFactory tenantDbContextFactory, TenantIdAccessor tenantIdAccessor)
    {
        _tenantDbContextFactory = tenantDbContextFactory;
        _tenantIdAccessor = tenantIdAccessor;
    }

    public TenantDbContext Create()
    {
        return _tenantDbContextFactory.Create(_tenantIdAccessor.Id);
    }
}
