using Concertable.B2B.Booking.Application.DTOs;
using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Booking.Domain.Lifecycle;

namespace Concertable.B2B.Booking.Application.Mappers;

internal static class BookingMappers
{
    extension(BookingEntity booking)
    {
        public BookingDto ToDto() => new(booking.Id, booking.State);
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

    extension(BookingState state)
    {
        public BookingStatus ToStatus() => state switch
        {
            BookingState.AwaitingConfirmation => BookingStatus.AwaitingConfirmation,
            BookingState.ConfirmationFailed => BookingStatus.ConfirmationFailed,
            BookingState.Confirmed => BookingStatus.Confirmed,
            BookingState.CancellationPending => BookingStatus.CancellationPending,
            BookingState.CancellationFailed => BookingStatus.CancellationFailed,
            BookingState.Cancelled => BookingStatus.Cancelled,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };
    }
}
