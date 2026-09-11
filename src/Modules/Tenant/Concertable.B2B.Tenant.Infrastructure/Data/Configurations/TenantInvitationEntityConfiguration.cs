using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Tenant.Infrastructure.Data.Configurations;

internal sealed class TenantInvitationEntityConfiguration : IEntityTypeConfiguration<TenantInvitationEntity>
{
    public void Configure(EntityTypeBuilder<TenantInvitationEntity> builder)
    {
        builder.ToTable(Schema.Tables.Invitations, Schema.Name);
        builder.HasKey(i => i.Id);
        builder.Property(i => i.TenantId).IsRequired();
        builder.Property(i => i.Email).IsRequired();
        builder.Property(i => i.Role).IsRequired();
        builder.Property(i => i.Status).IsRequired();
        builder.Property(i => i.CreatedByUserId).IsRequired();
        builder.Property(i => i.CreatedAt).IsRequired();
        builder.Property(i => i.ExpiresAt).IsRequired();

        // One live invite per (tenant, email); filtered on Pending so a revoked/expired one doesn't block a re-invite.
        builder.HasIndex(i => new { i.TenantId, i.Email })
            .IsUnique()
            .HasFilter($"[Status] = {(int)InvitationStatus.Pending}");

        // Registration-match lookup in TenantProvisioningHandler.
        builder.HasIndex(i => i.Email);
    }
}
