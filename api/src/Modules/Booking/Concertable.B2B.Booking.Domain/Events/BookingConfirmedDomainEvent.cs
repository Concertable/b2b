using Concertable.B2B.Booking.Contracts;
using Concertable.Kernel;

namespace Concertable.B2B.Booking.Domain.Events;

internal sealed record BookingConfirmedDomainEvent(ConfirmedBookingSnapshot Booking) : IDomainEvent;
