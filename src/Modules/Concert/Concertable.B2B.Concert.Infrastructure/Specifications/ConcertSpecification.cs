using Concertable.B2B.Concert.Domain.Entities;
using Concertable.Kernel.Specifications;

namespace Concertable.B2B.Concert.Infrastructure.Specifications;

internal sealed class ConcertSpecification : SpecificationBuilder<ConcertEntity>
{
    public static ISpecification<ConcertEntity> CreateWithArtistVenueAndBooking() =>
        new ConcertSpecification()
            .Include(concert => concert.Artist)
            .Include(concert => concert.Venue)
            .Include(concert => concert.Booking.Application);

    public static ISpecification<ConcertEntity> CreateWithVenue() =>
        new ConcertSpecification().Include(concert => concert.Venue);

    public static ISpecification<ConcertEntity> CreateWithBookingApplication() =>
        new ConcertSpecification().Include(concert => concert.Booking.Application);

    public static ISpecification<ConcertEntity, int?> CreateDealId() =>
        new ConcertSpecification().Select(concert => concert.Booking.Application.Opportunity.DealId);
}
