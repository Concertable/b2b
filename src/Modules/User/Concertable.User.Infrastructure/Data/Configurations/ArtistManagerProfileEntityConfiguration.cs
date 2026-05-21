using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.User.Infrastructure.Data.Configurations;

internal sealed class ArtistManagerProfileEntityConfiguration : IEntityTypeConfiguration<ArtistManagerProfileEntity>
{
    public void Configure(EntityTypeBuilder<ArtistManagerProfileEntity> builder)
    {
        builder.ToTable("ArtistManagerProfiles", "user");
        builder.HasKey(x => x.Sub);
        builder.Property(x => x.Sub).ValueGeneratedNever();
        builder.Property(x => x.ArtistId);
    }
}
