using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Tenant.Infrastructure.Data.Configurations;

internal sealed class TenantInvitationEntityConfiguration : IEntityTypeConfiguration<TenantInvitationEntity>
{
    public void Configure(EntityTypeBuilder<TenantInvitationEntity> builder)
    {
        builder.ToTable(Schema.Tables.Invitations, Schema.Name);
        builder.HasKey(i => i.Id);
        builder.HasAlternateKey(i => new { i.TenantId, i.Id });
        builder.Property(i => i.TenantId).IsRequired();
        builder.Property(i => i.Email).IsRequired();
        builder.Property(i => i.Status).IsRequired();
        builder.Property(i => i.InviterMembershipId).IsRequired();
        builder.Property(i => i.InviterPermissionVersion).IsRequired();
        builder.Property(i => i.InviterRolePolicyVersion).IsRequired();
        builder.HasMany(i => i.Assignments).WithOne()
            .HasForeignKey(row => new { row.TenantId, row.InvitationId })
            .HasPrincipalKey(i => new { i.TenantId, i.Id })
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(i => i.Assignments).HasField("assignments").AutoInclude();
        builder.Property(i => i.Version).IsRequired().IsConcurrencyToken();
        builder.Property(i => i.CreatedAt).IsRequired();
        builder.Property(i => i.ExpiresAt).IsRequired();

        // One live invite per (tenant, email); filtered on Pending so a revoked/expired one doesn't block a re-invite.
        builder.HasIndex(i => new { i.TenantId, i.Email })
            .IsUnique()
            .HasFilter($"\"Status\" = {(int)InvitationStatus.Pending}");

        // Registration-match lookup in TenantProvisioningHandler.
        builder.HasIndex(i => i.Email);
    }
}
