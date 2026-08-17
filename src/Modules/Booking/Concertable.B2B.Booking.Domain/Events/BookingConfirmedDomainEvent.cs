using Concertable.B2B.Booking.Contracts;
using Concertable.Kernel;

namespace Concertable.B2B.Booking.Domain.Events;

public sealed record BookingConfirmedDomainEvent(ConfirmedBooking Booking) : IDomainEvent;
