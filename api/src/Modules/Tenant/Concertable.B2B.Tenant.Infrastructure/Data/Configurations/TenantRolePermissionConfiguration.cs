using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Tenant.Infrastructure.Data.Configurations;

internal sealed class TenantRolePermissionConfiguration : IEntityTypeConfiguration<TenantRolePermission>
{
    public void Configure(EntityTypeBuilder<TenantRolePermission> builder)
    {
        builder.ToTable(Schema.Tables.RolePermissions, Schema.Name);
        builder.HasKey(row => new { row.TenantId, row.RoleId, row.PermissionKey });
        builder.Property(row => row.PermissionKey).HasMaxLength(100).IsRequired();
        builder.Property(row => row.Audience).IsRequired();
    }
}
