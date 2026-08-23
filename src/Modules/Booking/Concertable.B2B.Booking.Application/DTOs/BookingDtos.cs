using Concertable.B2B.Booking.Domain.State;

namespace Concertable.B2B.Booking.Application.DTOs;

internal abstract record BookingDto(int Id, BookingState State);

internal sealed record BookingSummaryDto(
    int Id,
    int ApplicationId,
    BookingState State,
    Guid OperationId,
    string? FailureCode,
    string? FailureMessage);

internal sealed record StandardBookingDto(int Id, BookingState State) : BookingDto(Id, State);

internal sealed record DeferredBookingDto(int Id, BookingState State, string PaymentMethodId)
    : BookingDto(Id, State);
