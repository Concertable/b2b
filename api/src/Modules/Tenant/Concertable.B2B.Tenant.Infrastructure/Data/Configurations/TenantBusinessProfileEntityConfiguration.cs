using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Tenant.Infrastructure.Data.Configurations;

internal sealed class TenantBusinessProfileEntityConfiguration : IEntityTypeConfiguration<TenantBusinessProfileEntity>
{
    public void Configure(EntityTypeBuilder<TenantBusinessProfileEntity> builder)
    {
        builder.ToTable(Schema.Tables.BusinessProfiles, Schema.Name);
        builder.HasKey(p => p.Id);
        builder.Property(p => p.TenantId).IsRequired();
        builder.Property(p => p.Kind).IsRequired();
        builder.Property(p => p.ActivatedAt).IsRequired();

        builder.HasIndex(p => new { p.TenantId, p.Kind }).IsUnique();
    }
}
