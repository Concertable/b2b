using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Tenant.Infrastructure.Data.Configurations;

internal sealed class TenantMembershipEntityConfiguration : IEntityTypeConfiguration<TenantMembershipEntity>
{
    public void Configure(EntityTypeBuilder<TenantMembershipEntity> builder)
    {
        builder.ToTable(Schema.Tables.Memberships, Schema.Name);
        builder.HasKey(m => m.Id);
        builder.HasAlternateKey(m => new { m.TenantId, m.Id });
        builder.Property(m => m.TenantId).IsRequired();
        builder.Property(m => m.UserId).IsRequired();
        builder.Property(m => m.PermissionVersion).IsRequired().IsConcurrencyToken();
        builder.Property(m => m.InvitedByMembershipId);
        builder.Property(m => m.CreatedAt).IsRequired();

        builder.HasMany(m => m.Assignments).WithOne()
            .HasForeignKey(row => new { row.TenantId, row.MembershipId })
            .HasPrincipalKey(m => new { m.TenantId, m.Id })
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(m => m.Assignments).HasField("assignments").AutoInclude();

        builder.HasIndex(m => new { m.TenantId, m.UserId }).IsUnique();
        builder.HasIndex(m => m.UserId);
    }
}
