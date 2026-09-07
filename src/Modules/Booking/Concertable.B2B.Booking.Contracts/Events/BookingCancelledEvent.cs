using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Booking.Contracts.Events;

[MessageType("concertable.b2b.booking-cancelled.v1")]
public sealed record BookingCancelledEvent(
    int BookingId,
    int ApplicationId,
    int OpportunityId) : IIntegrationEvent;
