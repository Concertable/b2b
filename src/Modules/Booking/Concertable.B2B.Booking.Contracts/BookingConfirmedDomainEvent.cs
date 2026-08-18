using Concertable.Kernel;

namespace Concertable.B2B.Booking.Contracts;

public sealed record BookingConfirmedDomainEvent(ConfirmedBooking Booking) : IDomainEvent;
