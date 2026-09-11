using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Tenant.Infrastructure.Data.Configurations;

internal sealed class TenantActivityEntityConfiguration : IEntityTypeConfiguration<TenantActivityEntity>
{
    public void Configure(EntityTypeBuilder<TenantActivityEntity> builder)
    {
        builder.ToTable(Schema.Tables.Activities, Schema.Name);
        builder.HasKey(a => a.Id);
        builder.HasIndex(a => new { a.TenantId, a.SourceKey }).IsUnique();
        builder.HasIndex(a => new { a.TenantId, a.At });
        builder.Property(a => a.SourceKey).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Subject).HasMaxLength(500).IsRequired();
        builder.Property(a => a.Detail).HasMaxLength(1000);
        builder.Property(a => a.Url).HasMaxLength(500).IsRequired();
    }
}
