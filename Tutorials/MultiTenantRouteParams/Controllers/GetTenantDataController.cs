using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiTenantRouteParams.Database;

namespace MultiTenantRouteParams.Controllers;

[ApiController]
public class GetTenantDataController : ControllerBase
{
    private readonly TenantDbContext _tenantDbContext;

    public GetTenantDataController(TenantDbContext tenantDbContext)
    {
        _tenantDbContext = tenantDbContext;
    }

    [HttpGet("/tenants/{tenantId:guid}/data")]
    public async Task<OkObjectResult> GetTenantDataAsync(CancellationToken cancellationToken)
    {
        var data = await _tenantDbContext.TenantData.ToArrayAsync(cancellationToken);
        return Ok(data);
    }
}
