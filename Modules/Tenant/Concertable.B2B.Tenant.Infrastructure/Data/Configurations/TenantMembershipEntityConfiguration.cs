using Concertable.B2B.Tenant.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Tenant.Infrastructure.Data.Configurations;

internal sealed class TenantMembershipEntityConfiguration : IEntityTypeConfiguration<TenantMembershipEntity>
{
    public void Configure(EntityTypeBuilder<TenantMembershipEntity> builder)
    {
        builder.ToTable(Schema.Tables.Memberships, Schema.Name);
        builder.HasKey(m => m.Id);
        builder.Property(m => m.TenantId).IsRequired();
        builder.Property(m => m.UserId).IsRequired();
        builder.Property(m => m.Role).IsRequired();
        builder.Property(m => m.CreatedAt).IsRequired();

        // One role per (tenant, user); UserId alone is the per-request membership lookup.
        builder.HasIndex(m => new { m.TenantId, m.UserId }).IsUnique();
        builder.HasIndex(m => m.UserId);
    }
}
