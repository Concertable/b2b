using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Tenant.Infrastructure.Data.Configurations;

internal sealed class SystemRolesFingerprintConfiguration : IEntityTypeConfiguration<SystemRolesFingerprint>
{
    public void Configure(EntityTypeBuilder<SystemRolesFingerprint> builder)
    {
        builder.ToTable(Schema.Tables.SystemRolesFingerprint, Schema.Name);
        builder.HasKey(state => state.Id);
        builder.Property(state => state.Value).IsRequired();
        builder.HasData(new { Id = 1, Value = SystemRoles.Fingerprint });
    }
}
