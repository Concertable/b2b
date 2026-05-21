using Concertable.Artist.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.Artist.Infrastructure.Data.Configurations;

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
    }
}

internal class ArtistGenreEntityConfiguration : IEntityTypeConfiguration<ArtistGenreEntity>
{
    public void Configure(EntityTypeBuilder<ArtistGenreEntity> builder)
    {
        builder.ToTable("ArtistGenres", Schema.Name);
        builder.HasKey(ag => new { ag.ArtistId, ag.Genre });
        builder.HasOne(ag => ag.Artist)
            .WithMany(a => a.ArtistGenres)
            .HasForeignKey(ag => ag.ArtistId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
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
