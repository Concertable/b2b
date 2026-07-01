using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.User.Infrastructure.Data.Configurations;

internal sealed class VenueManagerProfileEntityConfiguration : IEntityTypeConfiguration<VenueManagerProfileEntity>
{
    public void Configure(EntityTypeBuilder<VenueManagerProfileEntity> builder)
    {
        builder.ToTable(Schema.Tables.VenueManagerProfiles, Schema.Name);
        builder.HasKey(x => x.Sub);
        builder.Property(x => x.Sub).ValueGeneratedNever();
        builder.Property(x => x.VenueId);
    }
}
