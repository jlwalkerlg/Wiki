using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MultiTenantRouteParams.Database;

public class TenantDataConfiguration : IEntityTypeConfiguration<TenantData>
{
    public void Configure(EntityTypeBuilder<TenantData> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.Property(t => t.Value).IsRequired();
    }
}
