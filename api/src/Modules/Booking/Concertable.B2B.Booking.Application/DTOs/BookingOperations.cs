using Concertable.B2B.Booking.Domain.Lifecycle;

namespace Concertable.B2B.Booking.Application.DTOs;

internal sealed record BookingOperations(
    int Id,
    int ApplicationId,
    BookingState State,
    Guid OperationId,
    string? FailureCode,
    string? FailureMessage);
