using Microsoft.EntityFrameworkCore;
using MultiTenantRouteParams.Database;
using MultiTenantRouteParams.Exceptions;

namespace MultiTenantRouteParams.Routing;

public class TenantIdRouteMiddleware
{
    private readonly RequestDelegate _next;

    public TenantIdRouteMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        var tenantIdAccessor = httpContext.RequestServices.GetRequiredService<TenantIdAccessor>();
        await using var coreDbContext = httpContext.RequestServices.GetRequiredService<CoreDbContext>();

        var routeValue = httpContext.Request.RouteValues.FirstOrDefault(x => x.Key == "tenantId");
        if (routeValue.Value is null)
        {
            await _next(httpContext);
            return;
        }

        if (!Guid.TryParse(routeValue.Value.ToString(), out var tenantId))
        {
            await _next(httpContext);
            return;
        }

        var tenantExists = await coreDbContext.Tenants.AnyAsync(t => t.Id == tenantId, httpContext.RequestAborted);
        if (!tenantExists) throw new TenantNotFoundException();

        tenantIdAccessor.Id = tenantId;
        await _next(httpContext);
    }
}
