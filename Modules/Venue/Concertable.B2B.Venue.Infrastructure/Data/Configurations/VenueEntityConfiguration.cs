using Concertable.B2B.Venue.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Venue.Infrastructure.Data.Configurations;

internal sealed class VenueEntityConfiguration : IEntityTypeConfiguration<VenueEntity>
{
    public void Configure(EntityTypeBuilder<VenueEntity> builder)
    {
        builder.ToTable(Schema.Tables.Venues, Schema.Name);
        builder.Property(v => v.Location).HasColumnType("geography");
        builder.OwnsOne(v => v.Address, a =>
        {
            a.Property(x => x.County).HasColumnName("County");
            a.Property(x => x.Town).HasColumnName("Town");
        });
    }
}

internal sealed class VenueReviewConfiguration : IEntityTypeConfiguration<VenueReview>
{
    public void Configure(EntityTypeBuilder<VenueReview> builder)
    {
        builder.ToTable(Schema.Tables.VenueReviews, Schema.Name);
        builder.HasIndex(r => r.VenueId);
        builder.Property(r => r.Email).HasMaxLength(256).IsRequired();
    }
}

public sealed class VenueRatingProjectionConfiguration : IEntityTypeConfiguration<VenueRatingProjection>
{
    public void Configure(EntityTypeBuilder<VenueRatingProjection> builder)
    {
        builder.ToTable(Schema.Tables.VenueRatingProjections, Schema.Name);
        builder.HasKey(p => p.VenueId);
        builder.Property(p => p.VenueId).ValueGeneratedNever();
    }
}
