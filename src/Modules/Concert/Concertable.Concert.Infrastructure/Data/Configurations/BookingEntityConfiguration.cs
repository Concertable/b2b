using Concertable.Concert.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.Concert.Infrastructure.Data.Configurations;

internal class BookingEntityConfiguration : IEntityTypeConfiguration<BookingEntity>
{
    public void Configure(EntityTypeBuilder<BookingEntity> builder)
    {
        builder.ToTable("Bookings", Schema.Name);
        builder.HasOne(b => b.Application)
            .WithOne(a => a.Booking)
            .HasForeignKey<BookingEntity>(b => b.ApplicationId)
            .IsRequired()
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasDiscriminator<string>("Discriminator")
            .HasValue<StandardBooking>(nameof(StandardBooking))
            .HasValue<DeferredBooking>(nameof(DeferredBooking));
    }
}
