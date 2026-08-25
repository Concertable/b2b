using Concertable.B2B.Booking.Domain.Lifecycle;
using Concertable.B2B.Booking.Domain.Financial;

namespace Concertable.B2B.Booking.Application.DTOs;

internal abstract record BookingDto(int Id, State State);

internal sealed record BookingSummaryDto(
    int Id,
    int ApplicationId,
    State State,
    Guid OperationId,
    string? FailureCode,
    string? FailureMessage);

internal sealed record StandardBookingDto(int Id, State State) : BookingDto(Id, State);

internal sealed record DeferredBookingDto(int Id, State State, string PaymentMethodId)
    : BookingDto(Id, State);
