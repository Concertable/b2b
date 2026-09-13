using Concertable.B2B.Booking.Application.DTOs;
using Concertable.B2B.Booking.Application.Errors;
using Concertable.B2B.Booking.Application.Mappers;
using Concertable.B2B.Booking.Application.Models;

namespace Concertable.B2B.Booking.Infrastructure.Services;

internal sealed class BookingService : IBookingService
{
    private readonly IBookingRepository bookingRepository;
    private readonly IBookingWorkflow workflow;
    private readonly TimeProvider timeProvider;

    public BookingService(
        IBookingRepository bookingRepository,
        IBookingWorkflow workflow,
        TimeProvider timeProvider)
    {
        this.bookingRepository = bookingRepository;
        this.workflow = workflow;
        this.timeProvider = timeProvider;
    }

    public async Task<BookingDto?> GetByApplicationIdAsync(
        int applicationId,
        CancellationToken ct = default) =>
        (await bookingRepository.GetByApplicationIdAsync(applicationId, ct))?.ToDto();

    public Task<int?> GetIdByApplicationIdAsync(
        int applicationId,
        CancellationToken ct = default) =>
        bookingRepository.GetIdByApplicationIdAsync(applicationId, ct);

    public async Task<BookingSummaryDto?> GetSummaryByApplicationIdAsync(
        int applicationId,
        CancellationToken ct = default)
    {
        var booking = await bookingRepository.GetByApplicationIdAsync(applicationId, ct);
        return booking is null
            ? null
            : new BookingSummaryDto(
                booking.Id,
                booking.ApplicationId,
                booking.State,
                booking.OperationId,
                booking.FinancialFailure?.Code,
                booking.FinancialFailure?.Message);
    }

    public async Task<IReadOnlyList<BookingSummaryDto>> GetSummariesByApplicationIdsAsync(
        IReadOnlyCollection<int> applicationIds,
        CancellationToken ct = default) =>
        (await bookingRepository.GetByApplicationIdsAsync(applicationIds, ct))
            .Select(booking => new BookingSummaryDto(
                booking.Id,
                booking.ApplicationId,
                booking.State,
                booking.OperationId,
                booking.FinancialFailure?.Code,
                booking.FinancialFailure?.Message))
            .ToList();

    public Task<int> GetArtistAwaitingCheckoutCountAsync(
        Guid artistTenantId,
        CancellationToken ct = default) =>
        bookingRepository.GetAwaitingCheckoutCountByArtistTenantIdAsync(
            artistTenantId,
            timeProvider.GetUtcNow().UtcDateTime,
            ct);

    public Task<UnitResult<CancelBookingError>> CancelAsync(
        int bookingId,
        CancellationToken ct = default) =>
        workflow.CancelAsync(bookingId, ct);

    public Task RecordSucceededAsync(
        int bookingId,
        FinancialOperationSucceeded operation,
        CancellationToken ct = default) =>
        workflow.RecordSucceededAsync(bookingId, operation, ct);

    public Task RecordFailedAsync(
        int bookingId,
        FinancialOperationFailed operation,
        CancellationToken ct = default) =>
        workflow.RecordFailedAsync(bookingId, operation, ct);
}
