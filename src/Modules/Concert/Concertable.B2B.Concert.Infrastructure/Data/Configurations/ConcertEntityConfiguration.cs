using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Deal.Contracts.Enums;
using Concertable.B2B.DataAccess.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Concert.Infrastructure.Data.Configurations;

internal sealed class ConcertEntityConfiguration : IEntityTypeConfiguration<ConcertEntity>
{
    public void Configure(EntityTypeBuilder<ConcertEntity> builder)
    {
        builder.ToTable(Schema.Tables.Concerts, Schema.Name);
        builder.HasConcurrencyVersion();
        builder.Property(e => e.State).IsRequired().IsConcurrencyToken();
        builder.Property(e => e.SettlementGrossAmount).HasPrecision(18, 2);
        builder.ComplexProperty(e => e.SettlementPaymentReference, commitment =>
        {
            commitment.Property(value => value.OperationType).HasMaxLength(64);
            commitment.Property(value => value.ClientReference).HasMaxLength(256);
        });
        builder.ComplexProperty(e => e.FinancialFailure, failure =>
        {
            failure.Property(value => value.Code)
                .HasColumnName("FinancialFailureCode")
                .HasMaxLength(100);
            failure.Property(value => value.Message)
                .HasColumnName("FinancialFailureMessage")
                .HasMaxLength(1000);
        });
        builder.HasDiscriminator(e => e.DealType)
            .HasValue<FlatFeeConcert>(DealType.FlatFee)
            .HasValue<VenueHireConcert>(DealType.VenueHire)
            .HasValue<DoorSplitConcert>(DealType.DoorSplit)
            .HasValue<VersusConcert>(DealType.Versus);
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
