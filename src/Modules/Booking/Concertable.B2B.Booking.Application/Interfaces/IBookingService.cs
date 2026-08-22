using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Application.DTOs;
using Concertable.B2B.Booking.Application.Errors;
using Concertable.B2B.Booking.Application.Models;

namespace Concertable.B2B.Booking.Application.Interfaces;

internal interface IBookingService
{
    Task<StandardBookingDto> CreateStandardAsync(
        AcceptedApplication application,
        CancellationToken ct = default);
    Task<DeferredBookingDto> CreateDeferredAsync(
        AcceptedApplication application,
        string paymentMethodId,
        CancellationToken ct = default);
    Task<BookingDto?> GetByApplicationIdAsync(
        int applicationId,
        CancellationToken ct = default);
    Task<BookingSummaryDto?> GetSummaryByApplicationIdAsync(
        int applicationId,
        CancellationToken ct = default);
    Task<UnitResult<CancelBookingError>> CancelAsync(
        int bookingId,
        CancellationToken ct = default);
    Task RecordSucceededAsync(
        int bookingId,
        FinancialOperationSucceeded operation,
        CancellationToken ct = default);
    Task RecordFailedAsync(
        int bookingId,
        FinancialOperationFailed operation,
        CancellationToken ct = default);
}
