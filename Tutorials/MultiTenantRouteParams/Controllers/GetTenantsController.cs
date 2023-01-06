using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiTenantRouteParams.Database;

namespace MultiTenantRouteParams.Controllers;

[ApiController]
public class GetTenantsController : ControllerBase
{
    private readonly CoreDbContext _coreDbContext;

    public GetTenantsController(CoreDbContext coreDbContext)
	{
        _coreDbContext = coreDbContext;
    }

	[HttpGet("/tenants")]
	public async Task<OkObjectResult> GetTenantsAsync(CancellationToken cancellationToken)
	{
		var tenants = await _coreDbContext.Tenants.ToArrayAsync(cancellationToken);
		return Ok(tenants);
	}
}
