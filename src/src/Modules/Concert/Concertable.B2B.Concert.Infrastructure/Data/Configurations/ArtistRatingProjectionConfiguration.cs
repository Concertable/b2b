using Concertable.B2B.Artist.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Concert.Infrastructure.Data.Configurations;

/// <summary>Another module's projection table, mapped read-only for joins — never migrated from here.</summary>
internal sealed class ArtistRatingProjectionConfiguration : IEntityTypeConfiguration<ArtistRatingProjection>
{
    public void Configure(EntityTypeBuilder<ArtistRatingProjection> builder)
    {
        builder.ToTable("ArtistRatingProjections", "artist", t => t.ExcludeFromMigrations());
        builder.HasKey(p => p.ArtistId);
        builder.Property(p => p.ArtistId).ValueGeneratedNever();
    }
}
