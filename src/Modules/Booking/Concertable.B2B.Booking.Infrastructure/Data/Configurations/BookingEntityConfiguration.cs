using Concertable.B2B.Booking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Booking.Infrastructure.Data.Configurations;

internal sealed class BookingEntityConfiguration : IEntityTypeConfiguration<BookingEntity>
{
    public void Configure(EntityTypeBuilder<BookingEntity> builder)
    {
        builder.ToTable(Schema.Tables.Bookings, Schema.Name);
        builder.Property(booking => booking.State).IsRequired();
        builder.Property(booking => booking.ExpectedFinancialOperation).IsRequired();
        builder.Property(booking => booking.FinancialOperationReferenceId).HasMaxLength(255);
        builder.Property(booking => booking.FinancialFailureCode).HasMaxLength(100);
        builder.Property(booking => booking.FinancialFailureMessage).HasMaxLength(1000);
        builder.PrimitiveCollection(booking => booking.Genres);
        builder.HasIndex(booking => booking.ApplicationId).IsUnique();
        builder.HasIndex(booking => booking.OperationId).IsUnique();
        builder.HasIndex(booking => booking.CancellationOperationId)
            .IsUnique()
            .HasFilter("[CancellationOperationId] IS NOT NULL");
        builder.HasDiscriminator<string>("Discriminator")
            .HasValue<StandardBooking>(nameof(StandardBooking))
            .HasValue<DeferredBooking>(nameof(DeferredBooking));
    }
}
