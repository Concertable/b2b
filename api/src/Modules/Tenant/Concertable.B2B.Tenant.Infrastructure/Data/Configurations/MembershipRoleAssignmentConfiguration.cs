using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Tenant.Infrastructure.Data.Configurations;

internal sealed class MembershipRoleAssignmentConfiguration : IEntityTypeConfiguration<MembershipRoleAssignment>
{
    public void Configure(EntityTypeBuilder<MembershipRoleAssignment> builder)
    {
        builder.ToTable(Schema.Tables.MembershipRoleAssignments, Schema.Name);
        builder.HasKey(row => new { row.TenantId, row.MembershipId, row.RoleId });
        builder.HasOne<TenantRoleDefinition>().WithMany()
            .HasForeignKey(row => new { row.TenantId, row.RoleId })
            .HasPrincipalKey(role => new { role.TenantId, role.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(row => row.CreatedAt).IsRequired();
    }
}
