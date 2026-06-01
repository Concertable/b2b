using Concertable.B2B.Concert.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Concert.Infrastructure.Data.Configurations;

internal sealed class ConcertEntityConfiguration : IEntityTypeConfiguration<ConcertEntity>
{
    public void Configure(EntityTypeBuilder<ConcertEntity> builder)
    {
        builder.ToTable(Schema.Tables.Concerts, Schema.Name);
        builder.OwnsOne(e => e.Period, p =>
        {
            p.Property(x => x.Start).HasColumnName("StartDate");
            p.Property(x => x.End).HasColumnName("EndDate");
        });
        builder.HasOne(e => e.Booking)
            .WithOne(b => b.Concert)
            .HasForeignKey<ConcertEntity>(e => e.BookingId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(e => e.Artist)
            .WithMany()
            .HasForeignKey(e => e.ArtistId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(e => e.Venue)
            .WithMany()
            .HasForeignKey(e => e.VenueId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Property(e => e.Location).HasColumnType("geography");
        builder.PrimitiveCollection(e => e.Genres);
    }
}
