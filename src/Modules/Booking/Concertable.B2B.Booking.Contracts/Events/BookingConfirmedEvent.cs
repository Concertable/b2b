using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Booking.Contracts.Events;

[MessageType("concertable.b2b.booking-confirmed.v1")]
public sealed record BookingConfirmedEvent(ConfirmedBooking Booking) : IIntegrationEvent;
