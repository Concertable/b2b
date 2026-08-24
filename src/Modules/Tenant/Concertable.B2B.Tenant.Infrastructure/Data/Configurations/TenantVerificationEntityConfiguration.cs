using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Tenant.Infrastructure.Data.Configurations;

internal sealed class TenantVerificationEntityConfiguration : IEntityTypeConfiguration<TenantVerificationEntity>
{
    public void Configure(EntityTypeBuilder<TenantVerificationEntity> builder)
    {
        builder.ToTable(Schema.Tables.Verifications, Schema.Name);
        builder.HasKey(v => v.Id);
        builder.Property(v => v.TenantId).IsRequired();
        builder.Property(v => v.Status).IsRequired();
        builder.Property(v => v.RejectionReason).HasMaxLength(1000);
        builder.Property(v => v.SubmittedAt).IsRequired();

        builder.HasIndex(v => v.TenantId).IsUnique();

        builder.HasMany(v => v.Documents)
            .WithOne()
            .HasForeignKey(d => d.TenantVerificationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Metadata.FindNavigation(nameof(TenantVerificationEntity.Documents))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
