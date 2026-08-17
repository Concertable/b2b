using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.Kernel.ValueObjects;

namespace Concertable.B2B.Concert.UnitTests;

internal static class LifecycleTestFactory
{
    public static AcceptedApplication ToAccepted(this ApplicationEntity application) =>
        new(
            application.BeginAcceptance(),
            application.Id,
            application.OpportunityId,
            application.ArtistId,
            application.VenueTenantId,
            application.ArtistTenantId,
            application.DealType);

    public static ConfirmedBooking ToConfirmed(
        this BookingEntity booking,
        int venueId,
        DateRange period) =>
        new(
            booking.OperationId,
            booking.Id,
            booking.ApplicationId,
            booking.ArtistId,
            venueId,
            booking.VenueTenantId,
            booking.ArtistTenantId,
            booking.DealType,
            booking is DeferredBooking,
            period.Start,
            period.End);
}
