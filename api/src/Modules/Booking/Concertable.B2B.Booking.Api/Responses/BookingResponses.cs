using Concertable.B2B.Booking.Contracts;

namespace Concertable.B2B.Booking.Api.Responses;

public sealed record BookingOperationsResponse(
    int Id,
    int ApplicationId,
    BookingStatus Status,
    Guid OperationId,
    string? FailureCode,
    string? FailureMessage);
