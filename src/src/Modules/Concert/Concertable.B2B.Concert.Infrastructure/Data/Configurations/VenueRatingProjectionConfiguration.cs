using Concertable.B2B.Venue.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Concert.Infrastructure.Data.Configurations;

/// <summary>Another module's projection table, mapped read-only for joins — never migrated from here.</summary>
internal sealed class VenueRatingProjectionConfiguration : IEntityTypeConfiguration<VenueRatingProjection>
{
    public void Configure(EntityTypeBuilder<VenueRatingProjection> builder)
    {
        builder.ToTable("VenueRatingProjections", "venue", t => t.ExcludeFromMigrations());
        builder.HasKey(p => p.VenueId);
        builder.Property(p => p.VenueId).ValueGeneratedNever();
    }
}
