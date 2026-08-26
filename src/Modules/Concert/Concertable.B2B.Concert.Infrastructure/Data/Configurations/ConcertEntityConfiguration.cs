using Concertable.B2B.Concert.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Concert.Infrastructure.Data.Configurations;

internal sealed class ConcertEntityConfiguration : IEntityTypeConfiguration<ConcertEntity>
{
    public void Configure(EntityTypeBuilder<ConcertEntity> builder)
    {
        builder.ToTable(Schema.Tables.Concerts, Schema.Name);
        builder.Property(e => e.State).IsRequired();
        builder.Property(e => e.FinancialOperationReferenceId).HasMaxLength(255);
        builder.Property(e => e.FinancialFailureCode).HasMaxLength(100);
        builder.Property(e => e.FinancialFailureMessage).HasMaxLength(1000);
        builder.ComplexProperty(e => e.Period, p =>
        {
            p.Property(x => x.Start).HasColumnName("StartDate");
            p.Property(x => x.End).HasColumnName("EndDate");
        });
        builder.HasIndex(e => e.BookingId).IsUnique();
        builder.HasIndex(e => e.CancellationOperationId)
            .IsUnique()
            .HasFilter("[CancellationOperationId] IS NOT NULL");
        builder.HasIndex(e => e.SettlementOperationId)
            .IsUnique()
            .HasFilter("[SettlementOperationId] IS NOT NULL");

        builder.HasOne(e => e.Artist)
            .WithMany()
            .HasForeignKey(e => e.ArtistId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(e => e.Venue)
            .WithMany()
            .HasForeignKey(e => e.VenueId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.PrimitiveCollection(e => e.Genres);
    }
}
