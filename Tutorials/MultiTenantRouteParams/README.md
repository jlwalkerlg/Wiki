# MultiTenantRouteParams

This guide and the corresponding code sample show how to set up a multi-tenant architecture and configure routing such that the correct db context for each tenant is automatically injected into your controllers/services.

In a multi-tenant architecture, we have one core database which holds a record of each tenant, as well as separate databases for each tenant that store data specific to that tenant.

To set it up, we first define two db context classes -- a `CoreDbContext` for the core database and a `TenantDbContext` for the tenant databases.

```csharp
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
```

```csharp
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
```

The `CoreDbContext` is registered as usual in the dependency injection container.

```csharp
builder.Services.AddDbContext<CoreDbContext>(options =>
{
    var connectionString = new SqlConnectionStringBuilder
    {
        DataSource = builder.Configuration["Databases:Core:DataSource"],
        InitialCatalog = builder.Configuration["Databases:Core:InitialCatalog"],
        UserID = builder.Configuration["Databases:Core:UserID"],
        Password = builder.Configuration["Databases:Core:Password"],
        Encrypt = false,
    }.ConnectionString;

    options.UseSqlServer(connectionString);
});
```

However, the `TenantDbContext` can't be registered in this way since the `InitialCatalog` (the database name) is specific to each tenant and therefore only known when a request comes in from a particular tenant. Instead, we need to create a factory that can instantiate a new `TenantDbContext` for each tenant, and use the incoming HTTP request to decide which tenant to instantiate one for.

To do so, we first add some middleware that examines the route parameters and, if a `tenantId` parameter exists, checks the core database to see if the tenant exists. If so, it sets the `Id` property on the `TenantIdAccessor` object, which is scoped to the current HTTP request. The tenant ID can then be retrieved from the accessor later in the pipeline.

```csharp
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
```

This middleware is added to the HTTP request pipeline via the following statement.

```csharp
app.UseMiddleware<TenantIdRouteMiddleware>();
```

The `TenantIdAccessor` is registered as a scoped service since its `Id` property is scoped to each HTTP request.

```csharp
builder.Services.AddScoped<TenantIdAccessor>();
```

We then register a `RouteParamTenantDbContextFactory` as a scoped service, since one of its dependencies (`TenantIdAccessor`) is also scoped. Its `Create()` method takes the tenant ID from the `TenantIdAccessor` (which has already been populated by the middleware) and instantiates a new `TenantDbContext` specific to that tenant.

In this sample, it uses another factory to actually create the `TenantDbContext`, so we register that in the dependency injection container too.

```csharp
builder.Services.AddScoped<RouteParamTenantDbContextFactory>();
builder.Services.AddSingleton<TenantDbContextFactory>();
```

```csharp
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
```

To enable injection of the `TenantDbContext` into any scoped/transient service (including controllers), we then register a scoped factory function that resolves the `TenantDbContext` using the `RouteParamTenantDbContextFactory`.

```csharp
builder.Services.AddScoped(provider => provider.GetRequiredService<RouteParamTenantDbContextFactory>().Create());
```

The final piece is to ensure that a `tenantId` parameter exists in the route of any relevant endpoint, i.e. any endpoint that is specific to a particular tenant and where you may want to inject a `TenantDbContext`. See the `GetTenantDataController` as an example.

```csharp
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
```
