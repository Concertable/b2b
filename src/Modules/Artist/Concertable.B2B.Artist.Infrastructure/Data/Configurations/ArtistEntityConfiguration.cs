using Concertable.B2B.Artist.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Artist.Infrastructure.Data.Configurations;

internal sealed class ArtistEntityConfiguration : IEntityTypeConfiguration<ArtistEntity>
{
    public void Configure(EntityTypeBuilder<ArtistEntity> builder)
    {
        builder.ToTable(Schema.Tables.Artists, Schema.Name);
        builder.Property(a => a.Location).HasColumnType("geography");
        builder.OwnsOne(a => a.Address, a =>
        {
            a.Property(x => x.County).HasColumnName("County");
            a.Property(x => x.Town).HasColumnName("Town");
        });
        builder.PrimitiveCollection(a => a.Genres);
    }
}

internal sealed class ArtistReviewConfiguration : IEntityTypeConfiguration<ArtistReview>
{
    public void Configure(EntityTypeBuilder<ArtistReview> builder)
    {
        builder.ToTable(Schema.Tables.ArtistReviews, Schema.Name);
        builder.HasIndex(r => r.ArtistId);
        builder.Property(r => r.Email).HasMaxLength(256).IsRequired();
    }
}

public sealed class ArtistRatingProjectionConfiguration : IEntityTypeConfiguration<ArtistRatingProjection>
{
    public void Configure(EntityTypeBuilder<ArtistRatingProjection> builder)
    {
        builder.ToTable(Schema.Tables.ArtistRatingProjections, Schema.Name);
        builder.HasKey(p => p.ArtistId);
        builder.Property(p => p.ArtistId).ValueGeneratedNever();
    }
}
