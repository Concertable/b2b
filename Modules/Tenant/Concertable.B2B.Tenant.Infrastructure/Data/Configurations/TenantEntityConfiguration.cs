using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Tenant.Infrastructure.Data.Configurations;

internal sealed class TenantEntityConfiguration : IEntityTypeConfiguration<TenantEntity>
{
    public void Configure(EntityTypeBuilder<TenantEntity> builder)
    {
        builder.ToTable(Schema.Tables.Tenants, Schema.Name);
        builder.HasKey(o => o.Id);
        builder.Property(o => o.LegalName).IsRequired().HasMaxLength(200);
        builder.Property(o => o.CreatedAt).IsRequired();
    }
}
