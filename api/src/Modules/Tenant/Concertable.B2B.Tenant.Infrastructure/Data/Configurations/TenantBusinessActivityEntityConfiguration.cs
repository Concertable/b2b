using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Tenant.Infrastructure.Data.Configurations;

internal sealed class TenantBusinessActivityEntityConfiguration : IEntityTypeConfiguration<TenantBusinessActivityEntity>
{
    public void Configure(EntityTypeBuilder<TenantBusinessActivityEntity> builder)
    {
        builder.ToTable(Schema.Tables.BusinessActivities, Schema.Name);
        builder.HasKey(p => p.Id);
        builder.Property(p => p.TenantId).IsRequired();
        builder.Property(p => p.Kind).IsRequired();
        builder.Property(p => p.ActivatedAt).IsRequired();

        builder.HasIndex(p => new { p.TenantId, p.Kind }).IsUnique();
    }
}
