using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Application.Mappers;

internal static class BookingMappers
{
    public static StandardBookingDto ToDto(this StandardBooking booking) =>
        new(booking.Id);

    public static DeferredBookingDto ToDto(this DeferredBooking booking) =>
        new(booking.Id, booking.PaymentMethodId);

    public static IBooking ToDto(this BookingEntity booking) => booking switch
    {
        StandardBooking standard => standard.ToDto(),
        DeferredBooking deferred => deferred.ToDto(),
        _ => throw new InvalidOperationException($"Unknown booking type: {booking.GetType().Name}")
    };

    public static BookingSettlement ToSettlement(this DeferredBooking booking) =>
        new(
            booking.Id,
            booking.PaymentMethodId,
            booking.Application.Opportunity.Venue.UserId,
            booking.Application.Artist.UserId);
}
