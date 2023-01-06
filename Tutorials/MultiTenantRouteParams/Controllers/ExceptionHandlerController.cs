using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MultiTenantRouteParams.Exceptions;

namespace MultiTenantRouteParams.Controllers;

[ApiController]
public class ExceptionHandlerController : ControllerBase
{
    [Route("/Error")]
    public IActionResult HandleException()
    {
        var exceptionHandlerFeature = HttpContext.Features.Get<IExceptionHandlerFeature>();
        var exception = exceptionHandlerFeature?.Error;

        if (exception is null) return Ok();

        if (exception is TenantNotFoundException)
        {
            return Problem(statusCode: 404, title: "Tenant not found");
        }

        return Problem(statusCode: 500, title: "Internal server error");
    }
}
