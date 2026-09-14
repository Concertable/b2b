using Concertable.B2B.Booking.Domain.Entities;
using Concertable.Kernel.Specifications;

namespace Concertable.B2B.Booking.Infrastructure.Specifications;

internal sealed class BookingSpecification : SpecificationBuilder<BookingEntity>
{
    public static ISpecification<BookingEntity> CreateWithContract() =>
        new BookingSpecification().Include(booking => booking.Contract);
}
