using Concertable.B2B.Booking.Domain.Lifecycle;

namespace Concertable.B2B.Booking.Application.DTOs;

internal sealed record BookingDto(int Id, BookingState State);

internal sealed record BookingSummaryDto(
    int Id,
    int ApplicationId,
    BookingState State,
    Guid OperationId,
    string? FailureCode,
    string? FailureMessage);
