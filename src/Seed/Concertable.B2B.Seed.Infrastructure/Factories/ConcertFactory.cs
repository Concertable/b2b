using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Seed.Contracts.Specs;
using static Concertable.Seed.Identity.Extensions.EntityReflectionExtensions;

namespace Concertable.B2B.Seed.Infrastructure.Factories;

public static class ConcertFactory
{
    public static ConcertEntity Create(ConcertSeedSpec spec, BookingEntity booking)
    {
        var concert = ConcertEntity
            .CreateDraft(
                new ConfirmedBooking(
                    booking.OperationId,
                    booking.Id,
                    booking.ApplicationId,
                    spec.ArtistId,
                    spec.VenueId,
                    booking.VenueTenantId,
                    booking.ArtistTenantId,
                    booking.DealType,
                    spec.Period.Start,
                    spec.Period.End),
                spec.Name,
                spec.About,
                spec.Genres)
            .With(nameof(ConcertEntity.Id), spec.ConcertId)
            .With(nameof(ConcertEntity.Price), spec.Price)
            .With(nameof(ConcertEntity.TotalTickets), spec.TotalTickets)
            .With(nameof(ConcertEntity.TicketsSold), spec.TicketsSold);
        if (spec.DatePosted is not null)
            concert.Post(concert.Name, concert.About, concert.Price, concert.TotalTickets, spec.DatePosted.Value);
        return concert;
    }
}
