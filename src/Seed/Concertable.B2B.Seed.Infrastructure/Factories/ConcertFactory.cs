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
                    booking.OpportunityId,
                    spec.ArtistId,
                    spec.VenueId,
                    booking.VenueTenantId,
                    booking.ArtistTenantId,
                    booking.DealType,
                    booking.RequiresDoorRevenue,
                    spec.Period.Start,
                    spec.Period.End,
                    booking.Genres,
                    booking switch
                    {
                        StandardBooking when booking.DealType == DealType.FlatFee =>
                            new FlatFeeBookingTerms(((StandardBooking)booking).Amount),
                        StandardBooking =>
                            new VenueHireBookingTerms(((StandardBooking)booking).Amount),
                        DeferredBooking deferred when booking.DealType == DealType.DoorSplit =>
                            new DoorSplitBookingTerms(deferred.ArtistDoorPercent, deferred.PaymentMethodId),
                        DeferredBooking deferred =>
                            new VersusBookingTerms(
                                deferred.Guarantee,
                                deferred.ArtistDoorPercent,
                                deferred.PaymentMethodId),
                        _ => throw new ArgumentOutOfRangeException(nameof(booking), booking, null)
                    }),
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
