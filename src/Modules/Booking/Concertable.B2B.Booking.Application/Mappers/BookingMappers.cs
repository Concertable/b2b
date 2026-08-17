using Concertable.B2B.Booking.Application.DTOs;
using Concertable.B2B.Booking.Domain.Entities;

namespace Concertable.B2B.Booking.Application.Mappers;

internal static class BookingMappers
{
    public static BookingDto ToDto(this BookingEntity booking) => booking switch
    {
        StandardBooking standard => new StandardBookingDto(standard.Id, standard.State),
        DeferredBooking deferred => new DeferredBookingDto(
            deferred.Id,
            deferred.State,
            deferred.PaymentMethodId),
        _ => throw new InvalidOperationException($"Unknown booking type: {booking.GetType().Name}")
    };
}
