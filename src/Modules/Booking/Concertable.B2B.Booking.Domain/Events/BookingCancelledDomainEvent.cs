using Concertable.Kernel;

namespace Concertable.B2B.Booking.Domain.Events;

internal sealed record BookingCancelledDomainEvent(
    int BookingId,
    int ApplicationId,
    int OpportunityId) : IDomainEvent;
