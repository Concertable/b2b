using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Tenant.Infrastructure.Data.Configurations;

internal sealed class TenantRoleDefinitionConfiguration : IEntityTypeConfiguration<TenantRoleDefinition>
{
    public void Configure(EntityTypeBuilder<TenantRoleDefinition> builder)
    {
        builder.ToTable(Schema.Tables.RoleDefinitions, Schema.Name);
        builder.HasKey(role => role.Id);
        builder.HasAlternateKey(role => new { role.TenantId, role.Id });
        builder.Property(role => role.Name).IsRequired().HasMaxLength(100);
        builder.Property(role => role.SystemPresetKey).HasMaxLength(50);
        builder.Property(role => role.Version).IsConcurrencyToken();
        builder.HasIndex(role => new { role.TenantId, role.Name }).IsUnique();
        builder.HasIndex(role => new { role.TenantId, role.SystemPresetKey })
            .IsUnique().HasFilter("\"SystemPresetKey\" IS NOT NULL");
        builder.HasOne<TenantEntity>().WithMany().HasForeignKey(role => role.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(role => role.Permissions).WithOne()
            .HasForeignKey(row => new { row.TenantId, row.RoleId })
            .HasPrincipalKey(role => new { role.TenantId, role.Id })
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(role => role.Permissions).HasField("permissions").AutoInclude();
    }
}
