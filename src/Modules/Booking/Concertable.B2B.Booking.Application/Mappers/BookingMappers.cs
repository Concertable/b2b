using Concertable.B2B.Booking.Application.DTOs;
using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Booking.Domain.Lifecycle;
using Concertable.B2B.Booking.Domain.Financial;

namespace Concertable.B2B.Booking.Application.Mappers;

internal static class BookingMappers
{
    extension(BookingEntity booking)
    {
        public BookingDto ToDto() => booking switch
        {
            StandardBooking standard => new StandardBookingDto(standard.Id, standard.State),
            DeferredBooking deferred => new DeferredBookingDto(
                deferred.Id,
                deferred.State,
                deferred.PaymentMethodId),
            _ => throw new InvalidOperationException($"Unknown booking type: {booking.GetType().Name}")
        };
    }

    extension(BookingSummaryDto booking)
    {
        public BookingSummary ToSummary() =>
            new(
                booking.Id,
                booking.ApplicationId,
                booking.State.ToStatus(),
                booking.OperationId,
                booking.FailureCode,
                booking.FailureMessage);
    }

    extension(State state)
    {
        public BookingStatus ToStatus() => state switch
        {
            State.AwaitingConfirmation => BookingStatus.AwaitingConfirmation,
            State.ConfirmationFailed => BookingStatus.ConfirmationFailed,
            State.Confirmed => BookingStatus.Confirmed,
            State.CancellationPending => BookingStatus.CancellationPending,
            State.CancellationFailed => BookingStatus.CancellationFailed,
            State.Cancelled => BookingStatus.Cancelled,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };
    }
}
