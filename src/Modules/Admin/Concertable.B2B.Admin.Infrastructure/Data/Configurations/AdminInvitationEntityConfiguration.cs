using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Admin.Infrastructure.Data.Configurations;

internal sealed class AdminInvitationEntityConfiguration : IEntityTypeConfiguration<AdminInvitationEntity>
{
    public void Configure(EntityTypeBuilder<AdminInvitationEntity> builder)
    {
        builder.ToTable(Schema.Tables.AdminInvitations, Schema.Name);
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Email).IsRequired();
        builder.Property(i => i.Status).IsRequired();
        builder.Property(i => i.CreatedByUserId).IsRequired();
        builder.Property(i => i.CreatedAt).IsRequired();
        builder.Property(i => i.ExpiresAt).IsRequired();

        // One live invite per email; filtered on Pending so a revoked/expired one doesn't block a re-invite.
        builder.HasIndex(i => i.Email)
            .IsUnique()
            .HasFilter($"[Status] = {(int)AdminInvitationStatus.Pending}");
    }
}
