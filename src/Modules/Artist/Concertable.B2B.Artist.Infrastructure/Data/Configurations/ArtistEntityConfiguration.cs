using Concertable.B2B.Artist.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Artist.Infrastructure.Data.Configurations;

internal class ArtistEntityConfiguration : IEntityTypeConfiguration<ArtistEntity>
{
    public void Configure(EntityTypeBuilder<ArtistEntity> builder)
    {
        builder.ToTable("Artists", Schema.Name);
        builder.Property(a => a.Location).HasColumnType("geography");
        builder.OwnsOne(a => a.Address, a =>
        {
            a.Property(x => x.County).HasColumnName("County");
            a.Property(x => x.Town).HasColumnName("Town");
        });
        builder.PrimitiveCollection(a => a.Genres);
    }
}

public class ArtistRatingProjectionConfiguration : IEntityTypeConfiguration<ArtistRatingProjection>
{
    public void Configure(EntityTypeBuilder<ArtistRatingProjection> builder)
    {
        builder.ToTable("ArtistRatingProjections", Schema.Name);
        builder.HasKey(p => p.ArtistId);
        builder.Property(p => p.ArtistId).ValueGeneratedNever();
    }
}
