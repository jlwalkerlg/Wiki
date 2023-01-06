namespace MultiTenantRouteParams.Database;

public class TenantData
{
    public TenantData(Guid id)
    {
        Id = id;
    }

    public Guid Id { get; }

    public int Value { get; set; }
}
