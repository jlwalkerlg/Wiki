using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MultiTenantRouteParams.Database;
using MultiTenantRouteParams.Routing;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
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
builder.Services.AddSingleton<TenantDbContextFactory>();
builder.Services.AddScoped<RouteParamTenantDbContextFactory>();
builder.Services.AddScoped(provider => provider.GetRequiredService<RouteParamTenantDbContextFactory>().Create());
builder.Services.AddScoped<TenantIdAccessor>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

await InitialiseDatabase(app);

// Configure the HTTP request pipeline.
app.UseExceptionHandler("/Error");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseMiddleware<TenantIdRouteMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();

static async Task InitialiseDatabase(WebApplication app)
{
    using var scope = app.Services.CreateScope();

    await using var coreDbContext = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
    //await coreDbContext.Database.EnsureDeletedAsync();
    await coreDbContext.Database.EnsureCreatedAsync();

    var tenantDbContextFactory = scope.ServiceProvider.GetRequiredService<TenantDbContextFactory>();

    var tenants = await coreDbContext.Tenants.ToArrayAsync();

    var tenantNames = new[] { "TenantA", "TenantB", "TenantC" };
    foreach (var tenantName in tenantNames)
    {
        var tenant = tenants.FirstOrDefault(t => t.Name == tenantName);

        if (tenant is null)
        {
            tenant = new Tenant(Guid.NewGuid(), tenantName);
            await coreDbContext.Tenants.AddAsync(tenant);
            await coreDbContext.SaveChangesAsync();
        }

        await using var tenantDbContext = tenantDbContextFactory.Create(tenant.Id);
        //await tenantDbContext.Database.EnsureDeletedAsync();
        await tenantDbContext.Database.EnsureCreatedAsync();

        if (!await tenantDbContext.TenantData.AnyAsync())
        {
            var random = new Random();
            for (int i = 0; i < 5; i++)
            {
                await tenantDbContext.TenantData.AddAsync(new TenantData(Guid.NewGuid())
                {
                    Value = random.Next(1, 1000),
                });
            }
            await tenantDbContext.SaveChangesAsync();
        }
    }
}
