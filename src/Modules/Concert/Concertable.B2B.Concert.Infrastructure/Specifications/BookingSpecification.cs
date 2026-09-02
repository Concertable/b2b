using Concertable.B2B.Concert.Domain.Entities;
using Concertable.Kernel.Specifications;

namespace Concertable.B2B.Concert.Infrastructure.Specifications;

internal sealed class BookingSpecification : SpecificationBuilder<BookingEntity>
{
    public static ISpecification<BookingEntity, int?> CreateApplicationId() =>
        new BookingSpecification().Select(booking => booking.ApplicationId);
}
