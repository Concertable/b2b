using Concertable.Venue.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.Venue.Infrastructure.Data.Configurations;

internal class VenueEntityConfiguration : IEntityTypeConfiguration<VenueEntity>
{
    public void Configure(EntityTypeBuilder<VenueEntity> builder)
    {
        builder.ToTable("Venues", Schema.Name);
        builder.Property(v => v.Location).HasColumnType("geography");
        builder.OwnsOne(v => v.Address, a =>
        {
            a.Property(x => x.County).HasColumnName("County");
            a.Property(x => x.Town).HasColumnName("Town");
        });
    }
}

public class VenueRatingProjectionConfiguration : IEntityTypeConfiguration<VenueRatingProjection>
{
    public void Configure(EntityTypeBuilder<VenueRatingProjection> builder)
    {
        builder.ToTable("VenueRatingProjections", Schema.Name);
        builder.HasKey(p => p.VenueId);
        builder.Property(p => p.VenueId).ValueGeneratedNever();
    }
}
