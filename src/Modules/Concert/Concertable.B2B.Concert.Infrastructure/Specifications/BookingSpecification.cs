using Concertable.B2B.Concert.Domain.Entities;
using Concertable.Kernel.Specifications;

namespace Concertable.B2B.Concert.Infrastructure.Specifications;

internal sealed class BookingSpecification : SpecificationBuilder<BookingEntity>
{
    public static ISpecification<BookingEntity> CreateWithApplicationGraphAndConcert() =>
        new BookingSpecification()
            .Include(booking => booking.Application.Artist.Genres)
            .Include(booking => booking.Application.Opportunity.Venue)
            .Include(booking => booking.Concert);

    public static ISpecification<BookingEntity> CreateWithApplicationArtistAndVenue() =>
        new BookingSpecification()
            .Include(booking => booking.Application.Artist)
            .Include(booking => booking.Application.Opportunity.Venue);

    public static ISpecification<BookingEntity> CreateWithApplication() =>
        new BookingSpecification().Include(booking => booking.Application);

    public static ISpecification<BookingEntity, int?> CreateApplicationId() =>
        new BookingSpecification().Select(booking => booking.ApplicationId);

    public static ISpecification<BookingEntity, int?> CreateDealId() =>
        new BookingSpecification().Select(booking => booking.Application.Opportunity.DealId);
}
