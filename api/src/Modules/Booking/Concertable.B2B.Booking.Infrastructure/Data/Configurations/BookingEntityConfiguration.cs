using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.DataAccess.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Booking.Infrastructure.Data.Configurations;

internal sealed class BookingEntityConfiguration : IEntityTypeConfiguration<BookingEntity>
{
    public void Configure(EntityTypeBuilder<BookingEntity> builder)
    {
        builder.ToTable(Schema.Tables.Bookings, Schema.Name);
        builder.HasConcurrencyVersion();
        builder.Property(booking => booking.State).IsRequired().IsConcurrencyToken();
        builder.Property(booking => booking.ExpectedFinancialOperation).IsRequired();
        builder.ComplexProperty(booking => booking.FinancialFailure, failure =>
        {
            failure.Property(value => value.Code)
                .HasColumnName("FinancialFailureCode")
                .HasMaxLength(100);
            failure.Property(value => value.Message)
                .HasColumnName("FinancialFailureMessage")
                .HasMaxLength(1000);
        });
        builder.PrimitiveCollection(booking => booking.Genres);
        builder.HasIndex(booking => booking.ApplicationId).IsUnique();
        builder.OwnsRequiredClaim(booking => booking.Acceptance, "OperationId");
        builder.OwnsClaim(booking => booking.Cancellation, "CancellationOperationId");
    }
}
