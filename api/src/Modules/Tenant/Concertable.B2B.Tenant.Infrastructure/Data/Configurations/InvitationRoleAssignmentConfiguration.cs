using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Tenant.Infrastructure.Data.Configurations;

internal sealed class InvitationRoleAssignmentConfiguration : IEntityTypeConfiguration<InvitationRoleAssignment>
{
    public void Configure(EntityTypeBuilder<InvitationRoleAssignment> builder)
    {
        builder.ToTable(Schema.Tables.InvitationRoleAssignments, Schema.Name);
        builder.HasKey(row => new { row.TenantId, row.InvitationId, row.RoleId });
        builder.HasOne<TenantRoleDefinition>().WithMany()
            .HasForeignKey(row => new { row.TenantId, row.RoleId })
            .HasPrincipalKey(role => new { role.TenantId, role.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
