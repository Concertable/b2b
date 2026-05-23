using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.User.Infrastructure.Data.Configurations;

internal sealed class AdminProfileEntityConfiguration : IEntityTypeConfiguration<AdminProfileEntity>
{
    public void Configure(EntityTypeBuilder<AdminProfileEntity> builder)
    {
        builder.ToTable("AdminProfiles", "user");
        builder.HasKey(x => x.Sub);
        builder.Property(x => x.Sub).ValueGeneratedNever();
    }
}
